import { useQuery } from "@tanstack/react-query";
import { getDashboardCharts, type ChartDataResponse } from "../../../shared/api/dashboard";

export const useDashboardChartsQuery = () => {
  return useQuery<ChartDataResponse, Error>({
    queryKey: ["dashboard", "charts"],
    queryFn: getDashboardCharts,
  });
};