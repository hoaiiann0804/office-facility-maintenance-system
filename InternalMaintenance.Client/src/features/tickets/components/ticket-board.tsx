import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from "@tanstack/react-table";
import { useMemo } from "react";
import { Badge, EmptyState, Panel } from "../../../shared/ui";
import type { MaintenanceTicket } from "../../../entities/ticket/model/types";

type TicketBoardProps = {
  tickets: MaintenanceTicket[];
  selectedTicketId: number | null;
  onSelectTicket: (ticketId: number) => void;
};

const columnHelper = createColumnHelper<MaintenanceTicket>();

export function TicketBoard({ tickets, selectedTicketId, onSelectTicket }: TicketBoardProps) {
  const columns = useMemo(
    () => [
      columnHelper.accessor("ticketCode", {
        header: "Ticket",
        cell: (info) => <strong>{info.getValue()}</strong>,
      }),
      columnHelper.accessor("title", {
        header: "Title",
      }),
      columnHelper.accessor("priority", {
        header: "Priority",
        cell: (info) => (
          <Badge
            tone={
              info.getValue() === "Critical"
                ? "bad"
                : info.getValue() === "High"
                  ? "warn"
                  : info.getValue() === "Medium"
                    ? "primary"
                    : "default"
            }
          >
            {info.getValue()}
          </Badge>
        ),
      }),
      columnHelper.accessor("status", {
        header: "Status",
        cell: (info) => (
          <Badge
            tone={
              info.getValue() === "Resolved" || info.getValue() === "Closed"
                ? "good"
                : info.getValue() === "Cancelled"
                  ? "bad"
                  : "warn"
            }
          >
            {info.getValue()}
          </Badge>
        ),
      }),
    ],
    [],
  );

  // TanStack Table intentionally returns a non-memoizable table instance.
  // The hook is safe here because the table is re-created from props.
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: tickets,
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  return (
    <Panel>
      <span className="eyebrow">Tickets</span>
      <h2>Ticket queue</h2>
      <p className="section-lead">
        Bảng này dùng `@tanstack/react-table` để dễ scale khi thêm sorting, filtering, pagination.
      </p>

      <div className="table-wrap">
        <table className="table">
          <thead>
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <th key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            {table.getRowModel().rows.map((row) => (
              <tr
                key={row.id}
                style={{
                  background:
                    row.original.id === selectedTicketId
                      ? "rgba(125, 211, 252, 0.08)"
                      : "transparent",
                }}
                onClick={() => onSelectTicket(row.original.id)}
              >
                {row.getVisibleCells().map((cell) => (
                  <td key={cell.id}>{flexRender(cell.column.columnDef.cell, cell.getContext())}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {!tickets.length ? (
        <EmptyState title="Không có ticket" description="Chưa có dữ liệu ticket để hiển thị." />
      ) : null}
    </Panel>
  );
}
