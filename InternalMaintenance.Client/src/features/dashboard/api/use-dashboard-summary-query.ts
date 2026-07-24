import { useQuery } from "@tanstack/react-query";
import { getDashboardSummary, type DashboardSummaryResponse } from "../../../shared/api/dashboard";

export const useDashboardSummaryQuery = () => {
  return useQuery<DashboardSummaryResponse, Error>({
    queryKey: ["dashboard", "summary"],
    queryFn: getDashboardSummary,
  });
};