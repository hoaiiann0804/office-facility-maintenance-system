import { http } from "./http";
import type { Equipment, EquipmentQuery } from "../../entities/equipment/model/types";
import type { PagedResponse } from "../../entities/ticket/model/types";

export async function getEquipment(query: EquipmentQuery = {}) {
  const { data } = await http.get<PagedResponse<Equipment>>("/equipment", { params: query });
  return data;
}

export async function getEquipmentById(id: number) {
  const { data } = await http.get<Equipment>(`/equipment/${id}`);
  return data;
}
