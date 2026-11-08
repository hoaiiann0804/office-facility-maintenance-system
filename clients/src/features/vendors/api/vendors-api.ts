import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { http } from "../../../shared/api/http";
import type {
  CreateVendorRequest,
  UpdateVendorRequest,
  Vendor,
} from "../../../entities/vendor/model/types";

export const vendorsKeys = {
  all: ["vendors"] as const,
  lists: () => [...vendorsKeys.all, "list"] as const,
  list: (filters: string) => [...vendorsKeys.lists(), { filters }] as const,
};

export const fetchVendors = async (isActive?: boolean): Promise<Vendor[]> => {
  const { data } = await http.get<Vendor[]>("/vendors", {
    params: { isActive },
  });
  return data;
};

export const useVendors = (isActive?: boolean) => {
  return useQuery({
    queryKey: vendorsKeys.list(isActive !== undefined ? `isActive=${isActive}` : ""),
    queryFn: () => fetchVendors(isActive),
  });
};

export const createVendor = async (payload: CreateVendorRequest): Promise<Vendor> => {
  const { data } = await http.post<Vendor>("/vendors", payload);
  return data;
};

export const updateVendor = async (id: number, payload: UpdateVendorRequest): Promise<Vendor> => {
  const { data } = await http.put<Vendor>(`/vendors/${id}`, payload);
  return data;
};

export const toggleVendorActive = async (id: number): Promise<Vendor> => {
  const { data } = await http.patch<Vendor>(`/vendors/${id}/toggle-active`);
  return data;
};

export const useCreateVendorMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createVendor,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorsKeys.all });
    },
  });
};

export const useUpdateVendorMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: number; payload: UpdateVendorRequest }) =>
      updateVendor(id, payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorsKeys.all });
    },
  });
};

export const useToggleVendorActiveMutation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: toggleVendorActive,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: vendorsKeys.all });
    },
  });
};
