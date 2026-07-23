using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using InternalMaintenance.Api.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace InternalMaintenance.Api.Modules.TicketAttachments.Storage;

public sealed class R2AttachmentStorageService : IAttachmentStorageService
{
    // Vì Service cần gọi HTTP trực tới R2 để: 
    //HEAD kiểm tra object
    //DELETE xóa object
    //IHttpClientFactory là cách chuẩn của .NET để
    // TRánh tạo HttpClient tùy tiện
    // Quản lý connection tốt hơn
    // TRánh socket exhaustion
    private readonly IHttpClientFactory _httpClientFactory;

    // Để đọc cấu cấu hình R2 từ ngoài vào thei patten options
    private readonly R2AttachmentStorageOptions _options;

    public R2AttachmentStorageService(
        IHttpClientFactory httpClientFactory,
        IOptions<R2AttachmentStorageOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    // Hàm này tạo presigned URL cho upload, tức là URL tạm thời để frontend upload file trực tiếp lên R2 bằng PUT.
    // Vì backend không muốn nhận file bytes qua API
    // Thay vào đó : 
    // backend ký URL
    // frontend dùng URL đó upload thẳng lêb storage
    public Task<ServiceResult<string>> CreateUploadUrlAsync(
        string storageKey,
        string contentType)
    {
        // Tại sao không đưa contentType vào SignedHeaders khi upload?
        // Vì browser sẽ trigger CORS Preflight (OPTIONS) trước khi PUT.
        // Preflight không mang Content-Type header, nên chữ ký sẽ không khớp
        // và R2 trả về 403 SignatureDoesNotMatch → Frontend nhận "network error".
        // Giải pháp chuẩn cho browser upload: chỉ sign "host", không sign "content-type".
        var result = BuildPresignedUrl(
            httpMethod: HttpMethod.Put.Method,
            storageKey: storageKey,
            contentType: null);

        return Task.FromResult(result.IsSuccess
            ? ServiceResult<string>.Success(result.Data!)
            : ServiceResult<string>.Fail(result.StatusCode, result.Message!));
    }

    // Hàm này tạo URL tải xuống tạm thời bằng GET
    // Tạo sao ko trả file trực tiếp ? 
    // Vì file nằm ở storage
    //backend chỉ nên đóng vai trò kiểm soát quyền 
    // frontend tải trực tiếp từ storage sẽ nhẹ hơn API server

    // Ý nghĩa nghiệp vụ 
    // Khi user bấm dowload: 
    // backend kiểm tra quyền 
    // backend tạo URL tạm 
    // Client dùng URL đó tải file
    public Task<ServiceResult<string>> CreateDownloadUrlAsync(string storageKey)
    {
        var result = BuildPresignedUrl(
            httpMethod: HttpMethod.Get.Method,
            storageKey: storageKey,
            contentType: null);

        return Task.FromResult(result.IsSuccess
            ? ServiceResult<string>.Success(result.Data!)
            : ServiceResult<string>.Fail(result.StatusCode, result.Message!));
    }

    // Hàm này kiểm tra object có thật trong storage hay chưa
    // cách làm : tạo presinged URL cho HEAD 
    // GỬi request HEADD
    // Nếu storage trả 404 thì object ko tồn tại 
    // Nếu trả success thì object có thật

    // Vì sao cần ?
    // TRong bước confirm phải chắc chắc: 
    // - file đã upload xong 
    // - file có thật trong bucket 
    // Nếu không kiểm tra, DB có thể lưu record cho file chưa tồn tại, gây lệch dữ liệu 

    // Ý nghĩa nghiệp vụ 
    // Đây là lớp xác thực chống tình trạng 
    // - Client gọi confirm sớm 
    // - upload thất bại nhưng vẫn confirm
    // lưu metadata "ảo" vào DB
    public async Task<ServiceResult<bool>> ObjectExistsAsync(string storageKey)
    {
        var urlResult = BuildPresignedUrl(
            httpMethod: HttpMethod.Head.Method,
            storageKey: storageKey,
            contentType: null);

        if (!urlResult.IsSuccess)
        {
            return ServiceResult<bool>.Fail(urlResult.StatusCode, urlResult.Message!);
        }

        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Head, urlResult.Data);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ServiceResult<bool>.Success(false);
        }

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResult<bool>.Fail(
                (int)response.StatusCode,
                $"Failed to verify object existence. Storage returned {(int)response.StatusCode}.");
        }

        return ServiceResult<bool>.Success(true);
    }

    // Hàm này xóa object trong storage bằng DELETE.
    // vì sao xử lý theo cách này?
    // Vì khi user xóa attachment: 
    // - backend cần xóa file thật ở bucket 
    // - rồi mới đánh dấu DB là đã xóa 

    // Nếu storage trả NotFound thì sao ? 
    // code đang coi là trạng thái chấp nhận đc và trả 204 NoContent
    // Điều này hợp lý: 
    //- file đã ko còn mục tiêu xóa vật lý xem như hoàn thành 
    //- DB có thể chuyển sang soft delete

    // Ý nghĩa nghiệp vụ :
    // Đây là cách đảm bảo: 
    // - dữ liệu storage và DB ko lệch quá nhiều 
    // - file ko còn truy cập đc sau khi xóa 
    public async Task<ServiceResult> DeleteObjectAsync(string storageKey)
    {
        var urlResult = BuildPresignedUrl(
            httpMethod: HttpMethod.Delete.Method,
            storageKey: storageKey,
            contentType: null);

        if (!urlResult.IsSuccess)
        {
            return ServiceResult.Fail(urlResult.StatusCode, urlResult.Message!);
        }

        using var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Delete, urlResult.Data);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ServiceResult.Success(StatusCodes.Status204NoContent);
        }

        if (!response.IsSuccessStatusCode)
        {
            return ServiceResult.Fail(
                (int)response.StatusCode,
                $"Failed to delete object. Storage returned {(int)response.StatusCode}.");
        }

        return ServiceResult.Success(StatusCodes.Status204NoContent);
    }


    //Đây là hàm lõi tạo chữ ký presigned URL
    // Tại sao phải có ?
    // Vì R2 là storage tương thích S3, nên để upload/downloaf trực tiếp , URL phải đc ký bằng cơ chế AWS Signature.
    // Hàm này làm các việc: 
    // - kiểm tra config có đủ chưa
    // - Tính thời gian hết hạn
    // - tạo cannonical request 
    // - tạo string to sign
    // - sinh singning key 
    // - Tạo signature
    // - Ghép URL cuối cùng 

    // Ý tưởng nghiệp vụ 
    // Nó biến thông tin: bucket, object key , method PUT/GET/HEAD/DELETE, content type, secret key
    // Thành một URL hợp lệ chỉ dùng tạm trong thời gian giới hạn

    private ServiceResult<string> BuildPresignedUrl(
        string httpMethod,
        string storageKey,
        string? contentType)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.BucketName) ||
            string.IsNullOrWhiteSpace(_options.AccessKeyId) ||
            string.IsNullOrWhiteSpace(_options.SecretAccessKey))
        {
            return ServiceResult<string>.Fail(
                StatusCodes.Status500InternalServerError,
                "R2 storage configuration is missing");
        }

        var expiresSeconds = Math.Clamp(_options.PresignExpiresMinutes, 1, 7 * 24 * 60) * 60;
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var region = string.IsNullOrWhiteSpace(_options.Region) ? "auto" : _options.Region.Trim();

        var host = GetHost(_options.Endpoint);
        var canonicalUri = $"/{EncodePathSegment(_options.BucketName)}/{EncodeKeyPath(storageKey)}";

        var credentialScope = $"{dateStamp}/{region}/s3/aws4_request";
        var credential = $"{_options.AccessKeyId}/{credentialScope}";

        var signedHeaders = contentType is null
            ? "host"
            : "content-type;host";

        var queryParameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = credential,
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = expiresSeconds.ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = signedHeaders
        };

        var canonicalQueryString = string.Join("&",
            queryParameters.Select(pair =>
                $"{EncodeQueryComponent(pair.Key)}={EncodeQueryComponent(pair.Value)}"));

        var canonicalHeaders = new StringBuilder();
        canonicalHeaders.Append("host:").Append(host).Append('\n');
        if (contentType is not null)
        {
            canonicalHeaders.Insert(0, $"content-type:{contentType.Trim()}\n");
        }

        var payloadHash = "UNSIGNED-PAYLOAD";
        var canonicalRequest = string.Join(
            "\n",
            httpMethod,
            canonicalUri,
            canonicalQueryString,
            canonicalHeaders.ToString(),
            signedHeaders,
            payloadHash);

        var canonicalRequestHash = ToHexString(HashSha256(Encoding.UTF8.GetBytes(canonicalRequest)));
        var stringToSign = string.Join(
            "\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            canonicalRequestHash);

        var signingKey = GetSigningKey(_options.SecretAccessKey, dateStamp, region);
        var signature = ToHexString(HmacSha256(signingKey, stringToSign));

        var finalQueryString = $"{canonicalQueryString}&X-Amz-Signature={signature}";
        var presignedUrl = $"{_options.Endpoint.TrimEnd('/')}/{EncodePathSegment(_options.BucketName)}/{EncodeKeyPath(storageKey)}?{finalQueryString}";

        return ServiceResult<string>.Success(presignedUrl);
    }

    private static string GetHost(string endpoint)
    {
        return new Uri(endpoint).Host;
    }

    private static byte[] GetSigningKey(string secretKey, string dateStamp, string region)
    {
        var keyDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
        var keyRegion = HmacSha256(keyDate, region);
        var keyService = HmacSha256(keyRegion, "s3");
        return HmacSha256(keyService, "aws4_request");
    }

    private static byte[] HashSha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
    }

    private static byte[] HmacSha256(byte[] key, string value)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static string ToHexString(byte[] data)
    {
        return Convert.ToHexString(data).ToLowerInvariant();
    }

    private static string EncodeQueryComponent(string value)
    {
        return Uri.EscapeDataString(value)
            .Replace("+", "%20")
            .Replace("*", "%2A")
            .Replace("%7E", "~");
    }

    private static string EncodePathSegment(string value)
    {
        return string.Join(
            "/",
            value.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => Uri.EscapeDataString(segment)
                    .Replace("+", "%20")
                    .Replace("*", "%2A")
                    .Replace("%7E", "~")));
    }

    private static string EncodeKeyPath(string storageKey)
    {
        return string.Join(
            "/",
            storageKey.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => Uri.EscapeDataString(segment)
                    .Replace("+", "%20")
                    .Replace("*", "%2A")
                    .Replace("%7E", "~")));
    }
}
