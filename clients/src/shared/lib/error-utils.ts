import axios from "axios";

/**
 * Bảng dịch & chuẩn hóa các thông báo lỗi kỹ thuật/tiếng Anh từ API sang tiếng Việt thân thiện với người dùng cuối.
 */
const KNOWN_ERROR_MESSAGES: Record<string, string> = {
  // Equipment
  "Cannot delete equipment because it related maintenance tickets":
    "Không thể xóa thiết bị này vì đã có lịch sử phiếu bảo trì liên quan. Bạn có thể đổi trạng thái sang 'Thanh lý' thay vì xóa.",
  "Cannot retire equipment while it has open mantenance tickets":
    "Không thể thanh lý thiết bị khi vẫn còn phiếu sự cố/bảo trì chưa hoàn tất.",
  "Cannot retire equipment while it has open maintenance tickets":
    "Không thể thanh lý thiết bị khi vẫn còn phiếu sự cố/bảo trì chưa hoàn tất.",
  "Equipment code already exists":
    "Mã thiết bị đã tồn tại trong hệ thống. Vui lòng sử dụng mã khác.",
  "Equipment code cannot be changed after creation":
    "Không thể thay đổi mã thiết bị sau khi đã tạo.",
  "Equipment not found": "Không tìm thấy thông tin thiết bị.",
  "Equipment Not Found": "Không tìm thấy thông tin thiết bị.",
  "Invalid equipment status": "Trạng thái thiết bị không hợp lệ.",
  "Purchased date cannot be in the future": "Ngày mua thiết bị không được lớn hơn ngày hiện tại.",
  "This equipment is self-managed and does not create maintenance tickets":
    "Thiết bị này thuộc nhóm tự quản lý, không áp dụng tạo phiếu bảo trì.",
  "This equipment already has an open maintenance ticket":
    "Thiết bị này đang có một phiếu bảo trì chưa hoàn thành.",
  "Equipment is already under maintenance": "Thiết bị này hiện đang trong quá trình bảo trì.",

  // Departments
  "Cannot delete department because it has related users or equipment":
    "Không thể xóa phòng ban này vì đang có nhân sự hoặc thiết bị trực thuộc.",
  "Department name already exists": "Tên phòng ban đã tồn tại trên hệ thống.",
  "Department not found": "Không tìm thấy thông tin phòng ban.",
  "Department does not exist": "Phòng ban được chọn không tồn tại.",

  // Users
  "User created exists ": "Địa chỉ email này đã được sử dụng bởi một tài khoản khác.",
  "User already exists": "Địa chỉ email này đã tồn tại trên hệ thống.",
  "User not found": "Không tìm thấy thông tin người dùng.",
  "Creating a new Admin account is not allowed.":
    "Không được phép tạo thêm tài khoản quản trị viên (Admin).",
  "Cannot change email after user creation": "Không thể thay đổi email sau khi đã tạo tài khoản.",
  "Cannot modify role of Admin account": "Không thể thay đổi vai trò của tài khoản Admin.",
  "Cannot change role to Admin": "Không được phép nâng quyền tài khoản lên Admin.",
  "Cannot deactivate the last Admin account":
    "Không thể khóa tài khoản Admin duy nhất của hệ thống.",
  "Role not found": "Vai trò người dùng không hợp lệ.",
  "Temporary password must be at least 8 characters": "Mật khẩu tạm thời phải có ít nhất 8 ký tự.",

  // Vendors
  "A vendor with this name already exists": "Tên đối tác này đã tồn tại trên hệ thống.",
  "Vendor name already exists": "Tên đối tác này đã tồn tại trên hệ thống.",
  "Vendor not found": "Không tìm thấy thông tin đối tác.",
  "Cannot delete vendor because it has related maintenance tickets":
    "Không thể xóa đối tác vì đang có phiếu bảo trì liên quan. Bạn có thể tắt kích hoạt đối tác.",

  // Tickets & SLA
  "Ticket not found": "Không tìm thấy thông tin phiếu yêu cầu.",
  "Cannot create ticket for retired equipment":
    "Không thể tạo phiếu sự cố cho thiết bị đã thanh lý.",
  "Resolution note is required when resolving ticket":
    "Vui lòng nhập kết quả xử lý trước khi hoàn thành phiếu.",
  "Cancellation reason is required when cancelling ticket":
    "Vui lòng cung cấp lý do hủy phiếu yêu cầu.",
  "Vendor is required when transferring to vendor": "Vui lòng chọn đối tác sửa chữa bên ngoài.",
  "Estimated return date cannot be in the past":
    "Ngày dự kiến bàn giao trả không được ở trong quá khứ.",
  "Invalid status transition": "Chuyển đổi trạng thái không hợp lệ theo quy trình.",
  "Assigned technician not found": "Không tìm thấy kỹ thuật viên được chỉ định.",

  // Auth & Passwords
  "Invalid email or password": "Email hoặc mật khẩu không chính xác.",
  "Current password is incorrect": "Mật khẩu hiện tại không chính xác.",
  "New password must be different from current password":
    "Mật khẩu mới không được trùng với mật khẩu hiện tại.",
  "Your account has been deactivated. Please contact the administrator.":
    "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Quản trị viên.",
  "Google authentication verification failed.": "Xác thực tài khoản Google không thành công.",
  "Invalid Google authentication token": "Mã xác thực Google không hợp lệ hoặc đã hết hạn.",
  "Google ID token is required.": "Thiếu mã xác thực từ Google.",
  "Refresh token expired": "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.",
  "Refresh token revoked": "Phiên làm việc không còn hiệu lực. Vui lòng đăng nhập lại.",
  "Refresh token not found": "Không tìm thấy phiên đăng nhập hợp lệ.",

  // File Attachments
  "Attachment not found": "Không tìm thấy file đính kèm.",
  "You do not have permission to access this attachment":
    "Bạn không có quyền truy cập file đính kèm này.",
  "You do not have permission to delete this attachment":
    "Bạn không có quyền xóa file đính kèm này.",
  "You do not have permission to add attachment to this ticket":
    "Bạn không có quyền đính kèm file vào phiếu yêu cầu này.",
  "File size exceeds maximum allowed limit (100MB)":
    "Dung lượng file vượt quá giới hạn 100MB cho phép.",
  "File type not supported": "Định dạng file không được hỗ trợ.",
  "Upload attachment is not allowed in current ticket status":
    "Không thể đính kèm thêm file khi phiếu đã hoàn tất hoặc bị hủy.",
};

/**
 * Trích xuất và chuyển đổi thông báo lỗi từ Axios / Backend sang tiếng Việt thân thiện.
 */
export function getFriendlyErrorMessage(
  error: unknown,
  fallbackMessage = "Thao tác thất bại. Vui lòng thử lại sau.",
): string {
  if (!error) return fallbackMessage;

  if (axios.isAxiosError(error)) {
    // 1. Kiểm tra HTTP Status Code đặc biệt
    if (error.response?.status === 403) {
      return "Bạn không có quyền thực hiện thao tác này.";
    }
    if (error.response?.status === 401) {
      return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
    }
    if (error.response?.status === 404) {
      const dataMsg = error.response.data?.message ?? error.response.data;
      if (typeof dataMsg === "string" && KNOWN_ERROR_MESSAGES[dataMsg.trim()]) {
        return KNOWN_ERROR_MESSAGES[dataMsg.trim()];
      }
      return "Dữ liệu không tồn tại hoặc đã bị xóa.";
    }
    if (error.response?.status === 500) {
      return "Máy chủ đang gặp sự cố. Vui lòng liên hệ bộ phận IT.";
    }
    if (error.code === "ERR_NETWORK") {
      return "Không thể kết nối tới máy chủ. Vui lòng kiểm tra lại kết nối mạng.";
    }

    // 2. Lấy thông điệp trả về từ backend
    const rawMsg =
      error.response?.data?.message ?? error.response?.data?.title ?? error.response?.data;
    if (typeof rawMsg === "string") {
      const trimmed = rawMsg.trim();
      if (KNOWN_ERROR_MESSAGES[trimmed]) {
        return KNOWN_ERROR_MESSAGES[trimmed];
      }

      // Kiểm tra xem có chứa từ khóa tiếng Anh phổ biến không
      for (const [enKey, viVal] of Object.entries(KNOWN_ERROR_MESSAGES)) {
        if (trimmed.toLowerCase().includes(enKey.toLowerCase())) {
          return viVal;
        }
      }

      // Nếu backend đã trả tiếng Việt thì giữ nguyên
      return trimmed;
    }
  }

  if (error instanceof Error && error.message) {
    if (KNOWN_ERROR_MESSAGES[error.message]) {
      return KNOWN_ERROR_MESSAGES[error.message];
    }
  }

  return fallbackMessage;
}
