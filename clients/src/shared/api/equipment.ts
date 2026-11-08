import { http } from "./http";
import type {
  Equipment,
  EquipmentQuery,
  CreateEquipmentRequest,
  UpdateEquipmentRequest,
} from "../../entities/equipment/model/types";
import type { PagedResponse } from "../../entities/ticket/model/types";

export async function getEquipment(query: EquipmentQuery = {}) {
  const { data } = await http.get<PagedResponse<Equipment>>("/equipment", { params: query });
  return data;
}

export async function getEquipmentById(id: number) {
  const { data } = await http.get<Equipment>(`/equipment/${id}`);
  return data;
}

export async function createEquipment(payload: CreateEquipmentRequest) {
  const { data } = await http.post<Equipment>("/equipment", payload);
  return data;
}

export async function updateEquipment(id: number, payload: UpdateEquipmentRequest) {
  const { data } = await http.put<Equipment>(`/equipment/${id}`, payload);
  return data;
}

export async function deleteEquipment(id: number) {
  await http.delete(`/equipment/${id}`);
}
