(function () {
  const seed = window.WireframeData;
  if (!seed) {
    return;
  }

  const clone = (value) => JSON.parse(JSON.stringify(value));
  const state = {
    view: "overview",
    currentUser: null,
    banner: {
      type: "info",
      message: "Sẵn sàng cho wireframe split view. Hãy đăng nhập để xem các màn hình.",
    },
    search: "",
    ticketStatus: "All",
    ticketPriority: "All",
    selectedTicketId: seed.tickets[0]?.id ?? null,
    departmentEditId: null,
    equipmentEditId: null,
    tickets: clone(seed.tickets),
    users: clone(seed.users),
    departments: clone(seed.departments),
    equipment: clone(seed.equipment),
  };

  const els = {};

  const $ = (id) => document.getElementById(id);

  const formatDateTime = (value) => {
    if (!value) return "N/A";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return new Intl.DateTimeFormat("vi-VN", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(date);
  };

  const formatDate = (value) => {
    if (!value) return "N/A";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    return new Intl.DateTimeFormat("vi-VN", {
      dateStyle: "medium",
    }).format(date);
  };

  const escapeHtml = (value) => String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");

  const getUser = () => state.currentUser;
  const getRole = () => getUser()?.role ?? "Guest";
  const canManageCatalog = () => getRole() === "Admin";
  const canCreateTicket = () => ["Admin", "Staff"].includes(getRole());

  const roleBadgeClass = (role) => {
    if (role === "Admin") return "chip primary";
    if (role === "Manager") return "chip warn";
    if (role === "Technician") return "chip good";
    return "chip";
  };

  const statusBadgeClass = (status) => {
    if (["Closed", "Resolved"].includes(status)) return "chip good";
    if (status === "InProgress") return "chip warn";
    if (status === "Cancelled") return "chip bad";
    if (status === "UnderMaintenance") return "chip warn";
    return "chip primary";
  };

  const priorityBadgeClass = (priority) => {
    if (priority === "Critical") return "chip bad";
    if (priority === "High") return "chip warn";
    if (priority === "Medium") return "chip primary";
    return "chip";
  };

  const setBanner = (type, message) => {
    state.banner = { type, message };
    renderBanner();
  };

  const selectedTicket = () => state.tickets.find((ticket) => ticket.id === state.selectedTicketId) ?? state.tickets[0] ?? null;

  const departmentName = (departmentId) => state.departments.find((dept) => dept.id === departmentId)?.name ?? "N/A";

  const technicianUsers = () => state.users.filter((user) => user.role === "Technician" && user.isActive);

  const renderQuickLogins = () => {
    els.quickLogins.innerHTML = seed.quickLogins.map((item) => `
      <button type="button" class="list-item" data-quick-login="${escapeHtml(item.email)}">
        <div class="item-top">
          <div class="item-title">${escapeHtml(item.label)}</div>
          <span class="${roleBadgeClass(item.role)}">${escapeHtml(item.role)}</span>
        </div>
        <div class="item-meta">${escapeHtml(item.email)}</div>
        <div class="item-meta">${escapeHtml(item.hint)}</div>
      </button>
    `).join("");
  };

  const seedLoginDefaults = () => {
    const first = seed.quickLogins[0];
    els.loginEmail.value = first.email;
    els.loginPassword.value = first.password;
  };

  const toggleShell = (visible) => {
    els.appShell.hidden = !visible;
    els.hero.hidden = visible;
  };

  const login = (email, password) => {
    const user = state.users.find((item) => item.email.toLowerCase() === email.toLowerCase());
    if (!user || user.password !== password) {
      setBanner("error", "Email hoặc mật khẩu không đúng.");
      return;
    }

    state.currentUser = user;
    state.view = "overview";
    state.selectedTicketId = state.tickets[0]?.id ?? null;
    toggleShell(true);
    renderAll();
    setBanner("success", `Chào mừng ${user.fullName}. Đang mở dashboard theo vai trò ${user.role}.`);
  };

  const logout = () => {
    state.currentUser = null;
    toggleShell(false);
    seedLoginDefaults();
    setBanner("info", "Đã đăng xuất. Bạn có thể chọn quick login khác ở panel bên trái.");
  };

  const renderBanner = () => {
    if (els.banner) {
      els.banner.className = `banner ${state.banner.type}`;
      els.banner.textContent = state.banner.message;
    }
    if (els.loginBanner) {
      els.loginBanner.className = `banner ${state.banner.type}`;
      els.loginBanner.textContent = state.banner.message;
    }
  };

  const renderTabs = () => {
    const tabs = [
      { id: "overview", label: "Overview" },
      { id: "tickets", label: "Tickets" },
      { id: "catalog", label: "Catalog" },
      { id: "users", label: "Users" },
      { id: "api", label: "API map" },
    ];

    els.tabs.innerHTML = tabs.map((tab) => `
      <button type="button" class="tab ${state.view === tab.id ? "active" : ""}" data-view="${tab.id}">
        ${escapeHtml(tab.label)}
      </button>
    `).join("");
  };

  const renderPrimary = () => {
    const viewMap = {
      overview: renderOverviewView(),
      tickets: renderTicketsView(),
      catalog: renderCatalogView(),
      users: renderUsersView(),
      api: renderApiView(),
    };

    els.primary.innerHTML = viewMap[state.view] ?? viewMap.overview;
  };

  const renderInspector = () => {
    const user = getUser();
    const ticket = selectedTicket();

    els.inspector.innerHTML = `
      <div class="panel slim">
        <div class="eyebrow">Current persona</div>
        <h3 class="section-title" style="margin-top: 12px;">${escapeHtml(user?.fullName ?? "Guest")}</h3>
        <p class="section-subtitle">${escapeHtml(user?.email ?? "Not logged in yet")}</p>
        <div class="chip-row" style="margin-top: 14px;">
          <span class="${roleBadgeClass(user?.role ?? "Guest")}">${escapeHtml(user?.role ?? "Guest")}</span>
          <span class="chip">${escapeHtml(user?.departmentName ?? "No department")}</span>
          <span class="chip ${user?.mustChangePassword ? "warn" : "good"}">${user?.mustChangePassword ? "Must change password" : "Password ok"}</span>
        </div>
      </div>

      <div class="panel slim">
        <div class="eyebrow">Selected ticket</div>
        ${ticket ? `
          <h3 class="section-title" style="margin-top: 12px;">${escapeHtml(ticket.ticketCode)}</h3>
          <p class="section-subtitle">${escapeHtml(ticket.title)}</p>
          <div class="stack" style="margin-top: 14px;">
            <div class="chip-row">
              <span class="${statusBadgeClass(ticket.status)}">${escapeHtml(ticket.status)}</span>
              <span class="${priorityBadgeClass(ticket.priority)}">${escapeHtml(ticket.priority)}</span>
            </div>
            <div class="muted">Equipment: <strong>${escapeHtml(ticket.equipmentCode)}</strong> - ${escapeHtml(ticket.equipmentName)}</div>
            <div class="muted">Assigned: <strong>${escapeHtml(ticket.assignedTechnicianName ?? "Unassigned")}</strong></div>
            <div class="muted">Created: ${formatDateTime(ticket.createdAt)}</div>
          </div>
        ` : `<div class="empty">No ticket selected.</div>`}
      </div>

      <div class="panel slim">
        <div class="eyebrow">Split view note</div>
        <p class="section-subtitle">Mỗi màn hình được chia thành vùng list, vùng detail và vùng inspector để sau này map sang React component sẽ rất thẳng.</p>
      </div>

      ${state.view === "overview" ? `
        <div class="panel slim">
          <div class="eyebrow">Change password</div>
          <h3 class="section-title" style="margin-top: 12px;">Auth flow preview</h3>
          <form id="change-password-form" class="stack" style="margin-top: 14px;">
            <div class="field">
              <label>Current password</label>
              <input id="current-password" class="input" type="password" placeholder="Current password" />
            </div>
            <div class="field">
              <label>New password</label>
              <input id="new-password" class="input" type="password" placeholder="New password" />
            </div>
            <button type="submit" class="btn small">Save new password</button>
          </form>
        </div>
      ` : ""}
    `;
  };

  const renderOverviewView = () => {
    const openTickets = state.tickets.filter((ticket) => !["Closed", "Cancelled"].includes(ticket.status)).length;
    const activeEquipment = state.equipment.filter((item) => item.status === "Active").length;
    const technicians = state.users.filter((user) => user.role === "Technician").length;
    const departments = state.departments.length;

    const recentItems = [
      ...state.tickets.flatMap((ticket) => ticket.history.map((history) => ({
        kind: "status",
        text: `${ticket.ticketCode} -> ${history.status}`,
        detail: history.note || "Status changed",
        time: history.changedAt,
      }))),
      ...state.tickets.flatMap((ticket) => ticket.comments.map((comment) => ({
        kind: "comment",
        text: `${ticket.ticketCode} comment by ${comment.userName}`,
        detail: comment.content,
        time: comment.createdAt,
      }))),
    ]
      .sort((a, b) => new Date(b.time) - new Date(a.time))
      .slice(0, 6);

    return `
      <div class="view-shell">
        <div class="split">
          <div class="panel">
            <div class="eyebrow">Overview</div>
            <h2 class="section-title" style="margin-top: 12px;">Dashboard overview</h2>
            <p class="section-subtitle">A compact working model of the backend features. Good for reading, less noisy than a full production UI.</p>
            <div class="stats">
              <div class="stat"><div class="stat-label">Open tickets</div><div class="stat-value">${openTickets}</div></div>
              <div class="stat"><div class="stat-label">Active equipment</div><div class="stat-value">${activeEquipment}</div></div>
              <div class="stat"><div class="stat-label">Technicians</div><div class="stat-value">${technicians}</div></div>
              <div class="stat"><div class="stat-label">Departments</div><div class="stat-value">${departments}</div></div>
            </div>
          </div>

          <div class="panel">
            <div class="eyebrow">Workflow</div>
            <h2 class="section-title" style="margin-top: 12px;">Ticket life cycle</h2>
            <div class="stack" style="margin-top: 16px;">
              ${seed.workflow.map((step, index) => `
                <div class="list-item">
                  <div class="item-top">
                    <div class="item-title">${index + 1}. ${escapeHtml(step)}</div>
                    <span class="chip primary">${index === 0 ? "entry" : index === seed.workflow.length - 1 ? "finish" : "step"}</span>
                  </div>
                </div>
              `).join("")}
            </div>
          </div>
        </div>

        <div class="split">
          <div class="panel">
            <div class="eyebrow">Activity</div>
            <h2 class="section-title" style="margin-top: 12px;">Latest updates</h2>
            <div class="timeline" style="margin-top: 16px;">
              ${recentItems.map((item) => `
                <div class="timeline-item">
                  <strong>${escapeHtml(item.text)}</strong>
                  <span>${escapeHtml(item.detail)} · ${formatDateTime(item.time)}</span>
                </div>
              `).join("")}
            </div>
          </div>

          <div class="panel">
            <div class="eyebrow">Rules</div>
            <h2 class="section-title" style="margin-top: 12px;">Business guardrails</h2>
            <div class="list" style="margin-top: 16px;">
              <div class="list-item"><div class="item-title">No duplicate open ticket for one asset</div><div class="item-meta">The backend blocks ticket spam on active equipment.</div></div>
              <div class="list-item"><div class="item-title">Equipment code stays immutable</div><div class="item-meta">Edit form keeps the code as a protected field after creation.</div></div>
              <div class="list-item"><div class="item-title">Closed tickets are final</div><div class="item-meta">The workflow is intentionally strict so the history remains auditable.</div></div>
            </div>
          </div>
        </div>
      </div>
    `;
  };

  const ticketFilters = () => {
    const filtered = state.tickets.filter((ticket) => {
      const matchesSearch = !state.search || `${ticket.ticketCode} ${ticket.title} ${ticket.description}`.toLowerCase().includes(state.search.toLowerCase());
      const matchesStatus = state.ticketStatus === "All" || ticket.status === state.ticketStatus;
      const matchesPriority = state.ticketPriority === "All" || ticket.priority === state.ticketPriority;
      return matchesSearch && matchesStatus && matchesPriority;
    });

    return { filtered };
  };

  const renderTicketsView = () => {
    const { filtered } = ticketFilters();
    const ticket = selectedTicket();
    const technicians = technicianUsers();

    return `
      <div class="split">
        <div class="panel">
          <div class="eyebrow">Tickets</div>
          <h2 class="section-title" style="margin-top: 12px;">Ticket queue</h2>
          <p class="section-subtitle">Search, filter, select, then act on the ticket. The right column stays readable as the detail area.</p>

          <div class="grid-3" style="margin-top: 16px;">
            <div class="field">
              <label>Search</label>
              <input class="input" id="ticket-search" value="${escapeHtml(state.search)}" placeholder="Ticket code, title, description" />
            </div>
            <div class="field">
              <label>Status</label>
              <select class="select" id="ticket-status">
                ${["All", ...seed.workflow, "Cancelled"].map((status) => `<option value="${escapeHtml(status)}" ${state.ticketStatus === status ? "selected" : ""}>${escapeHtml(status)}</option>`).join("")}
              </select>
            </div>
            <div class="field">
              <label>Priority</label>
              <select class="select" id="ticket-priority">
                ${["All", ...seed.priorities].map((priority) => `<option value="${escapeHtml(priority)}" ${state.ticketPriority === priority ? "selected" : ""}>${escapeHtml(priority)}</option>`).join("")}
              </select>
            </div>
          </div>

          <div class="list" style="margin-top: 16px;">
            ${filtered.map((item) => `
              <button type="button" class="list-item ${item.id === ticket?.id ? "selected" : ""}" data-ticket-select="${item.id}">
                <div class="item-top">
                  <div>
                    <div class="item-title">${escapeHtml(item.ticketCode)}</div>
                    <div class="item-meta">${escapeHtml(item.title)}</div>
                  </div>
                  <div class="chip-row">
                    <span class="${statusBadgeClass(item.status)}">${escapeHtml(item.status)}</span>
                    <span class="${priorityBadgeClass(item.priority)}">${escapeHtml(item.priority)}</span>
                  </div>
                </div>
                <div class="item-meta">${escapeHtml(item.equipmentName)} · ${escapeHtml(item.createdByUserName)} · ${formatDateTime(item.createdAt)}</div>
              </button>
            `).join("")}
            ${filtered.length === 0 ? `<div class="empty">Không có ticket phù hợp bộ lọc hiện tại.</div>` : ""}
          </div>
        </div>

        <div class="panel">
          ${ticket ? `
            <div class="eyebrow">Detail</div>
            <h2 class="section-title" style="margin-top: 12px;">${escapeHtml(ticket.ticketCode)}</h2>
            <p class="section-subtitle">${escapeHtml(ticket.title)}</p>
            <div class="chip-row" style="margin-top: 14px;">
              <span class="${statusBadgeClass(ticket.status)}">${escapeHtml(ticket.status)}</span>
              <span class="${priorityBadgeClass(ticket.priority)}">${escapeHtml(ticket.priority)}</span>
              <span class="chip">Created by ${escapeHtml(ticket.createdByUserName)}</span>
            </div>

            <div class="stack" style="margin-top: 18px;">
              <div class="list-item">
                <div class="item-title">Description</div>
                <div class="item-meta">${escapeHtml(ticket.description)}</div>
              </div>
              <div class="list-item">
                <div class="item-title">Equipment</div>
                <div class="item-meta">${escapeHtml(ticket.equipmentCode)} - ${escapeHtml(ticket.equipmentName)}</div>
              </div>
              <div class="list-item">
                <div class="item-title">Current assignment</div>
                <div class="item-meta">${escapeHtml(ticket.assignedTechnicianName ?? "Unassigned")}</div>
              </div>
            </div>

            <div class="panel slim" style="margin-top: 18px; background: rgba(255,255,255,0.03);">
              <div class="field">
                <label>Assign technician</label>
                <select class="select" id="assign-technician">
                  <option value="">Select technician</option>
                  ${technicians.map((tech) => `<option value="${tech.id}" ${ticket.assignedTechnicianId === tech.id ? "selected" : ""}>${escapeHtml(tech.fullName)}</option>`).join("")}
                </select>
              </div>
              <div class="field" style="margin-top: 12px;">
                <label>Assignment note</label>
                <textarea class="textarea" id="assign-note" placeholder="Optional note for the history timeline"></textarea>
              </div>
              <div class="btn-row" style="margin-top: 12px;">
                <button type="button" class="btn small" id="assign-btn">Assign</button>
                <button type="button" class="btn secondary small" id="set-progress-btn">Mark In Progress</button>
                <button type="button" class="btn secondary small" id="resolve-btn">Resolve</button>
                <button type="button" class="btn secondary small" id="close-btn">Close</button>
                <button type="button" class="btn danger small" id="cancel-btn">Cancel</button>
              </div>
              <div class="field" style="margin-top: 12px;">
                <label>Resolution note</label>
                <textarea class="textarea" id="resolution-note" placeholder="Required when moving to Resolved"></textarea>
              </div>
            </div>

            <div class="panel slim" style="margin-top: 18px; background: rgba(255,255,255,0.03);">
              <div class="field">
                <label>Add comment</label>
                <textarea class="textarea" id="ticket-comment" placeholder="Leave a note on the ticket"></textarea>
              </div>
              <div class="btn-row" style="margin-top: 12px;">
                <button type="button" class="btn small" id="comment-btn">Post comment</button>
              </div>
            </div>
          ` : `<div class="empty">Chọn một ticket để xem chi tiết.</div>`}
        </div>
      </div>
    `;
  };

  const renderCatalogView = () => {
    return `
      <div class="split">
        <div class="panel">
          <div class="eyebrow">Departments</div>
          <h2 class="section-title" style="margin-top: 12px;">Department registry</h2>
          <p class="section-subtitle">Admin-only in the backend, with delete protection if users or equipment still depend on the department.</p>

          <form id="department-form" class="stack" style="margin-top: 16px;">
            <input type="hidden" id="department-id" value="${state.departmentEditId ?? ""}" />
            <div class="grid-2">
              <div class="field">
                <label>Name</label>
                <input class="input" id="department-name" placeholder="Facilities" />
              </div>
              <div class="field">
                <label>Description</label>
                <input class="input" id="department-description" placeholder="Short description" />
              </div>
            </div>
            <div class="btn-row">
              <button type="submit" class="btn small">Save department</button>
              <button type="button" class="btn secondary small" id="department-reset">Reset</button>
            </div>
          </form>

          <div class="list" style="margin-top: 16px;">
            ${state.departments.map((department) => {
              const userCount = state.users.filter((user) => user.departmentId === department.id).length;
              const eqCount = state.equipment.filter((item) => item.departmentId === department.id).length;
              return `
                <div class="list-item">
                  <div class="item-top">
                    <div>
                      <div class="item-title">${escapeHtml(department.name)}</div>
                      <div class="item-meta">${escapeHtml(department.description)}</div>
                    </div>
                    <div class="chip-row">
                      <span class="chip">${userCount} users</span>
                      <span class="chip">${eqCount} assets</span>
                    </div>
                  </div>
                  <div class="btn-row" style="margin-top: 10px;">
                    <button type="button" class="btn secondary small" data-department-edit="${department.id}">Edit</button>
                    <button type="button" class="btn danger small" data-department-delete="${department.id}">Delete</button>
                  </div>
                </div>
              `;
            }).join("")}
          </div>
        </div>

        <div class="panel">
          <div class="eyebrow">Equipment</div>
          <h2 class="section-title" style="margin-top: 12px;">Asset catalog</h2>
          <p class="section-subtitle">Equipment code is immutable after creation, and status transitions should remain aligned with the ticket flow.</p>

          <form id="equipment-form" class="stack" style="margin-top: 16px;">
            <input type="hidden" id="equipment-id" value="${state.equipmentEditId ?? ""}" />
            <div class="grid-2">
              <div class="field">
                <label>Code</label>
                <input class="input" id="equipment-code" placeholder="PRN-IT-001" />
              </div>
              <div class="field">
                <label>Name</label>
                <input class="input" id="equipment-name" placeholder="Printer model" />
              </div>
            </div>
            <div class="grid-2">
              <div class="field">
                <label>Department</label>
                <select class="select" id="equipment-department">
                  ${state.departments.map((department) => `<option value="${department.id}">${escapeHtml(department.name)}</option>`).join("")}
                </select>
              </div>
              <div class="field">
                <label>Status</label>
                <select class="select" id="equipment-status">
                  ${seed.equipmentStatuses.map((status) => `<option value="${escapeHtml(status)}">${escapeHtml(status)}</option>`).join("")}
                </select>
              </div>
            </div>
            <div class="grid-2">
              <div class="field">
                <label>Purchased date</label>
                <input class="input" id="equipment-purchased" type="date" />
              </div>
              <div class="field">
                <label>Description</label>
                <input class="input" id="equipment-description" placeholder="Short usage note" />
              </div>
            </div>
            <div class="btn-row">
              <button type="submit" class="btn small">Save equipment</button>
              <button type="button" class="btn secondary small" id="equipment-reset">Reset</button>
            </div>
          </form>

          <div class="list" style="margin-top: 16px;">
            ${state.equipment.map((item) => `
              <div class="list-item">
                <div class="item-top">
                  <div>
                    <div class="item-title">${escapeHtml(item.code)} · ${escapeHtml(item.name)}</div>
                    <div class="item-meta">${escapeHtml(item.departmentName)} · ${escapeHtml(item.description)} · ${formatDate(item.purchasedDate)}</div>
                  </div>
                  <div class="chip-row">
                    <span class="${statusBadgeClass(item.status)}">${escapeHtml(item.status)}</span>
                  </div>
                </div>
                <div class="btn-row" style="margin-top: 10px;">
                  <button type="button" class="btn secondary small" data-equipment-edit="${item.id}">Edit</button>
                  <button type="button" class="btn danger small" data-equipment-delete="${item.id}">Delete</button>
                </div>
              </div>
            `).join("")}
          </div>
        </div>
      </div>
    `;
  };

  const renderUsersView = () => {
    return `
      <div class="split">
        <div class="panel">
          <div class="eyebrow">Users</div>
          <h2 class="section-title" style="margin-top: 12px;">Directory snapshot</h2>
          <p class="section-subtitle">Read-only view for the demo, focused on role scope, department assignment, and password flags.</p>

          <table class="table" style="margin-top: 16px;">
            <thead>
              <tr>
                <th>Name</th>
                <th>Role</th>
                <th>Department</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              ${state.users.map((user) => `
                <tr>
                  <td>
                    <strong>${escapeHtml(user.fullName)}</strong><br />
                    <span class="muted">${escapeHtml(user.email)}</span>
                  </td>
                  <td><span class="${roleBadgeClass(user.role)}">${escapeHtml(user.role)}</span></td>
                  <td>${escapeHtml(user.departmentName)}</td>
                  <td>
                    <div class="stack">
                      <span class="${user.isActive ? "chip good" : "chip bad"}">${user.isActive ? "Active" : "Inactive"}</span>
                      <span class="${user.mustChangePassword ? "chip warn" : "chip good"}">${user.mustChangePassword ? "Must change password" : "Password ok"}</span>
                    </div>
                  </td>
                </tr>
              `).join("")}
            </tbody>
          </table>
        </div>

        <div class="panel">
          <div class="eyebrow">Role matrix</div>
          <h2 class="section-title" style="margin-top: 12px;">Permissions at a glance</h2>
          <div class="stack" style="margin-top: 16px;">
            <div class="list-item">
              <div class="item-title">Admin</div>
              <div class="item-meta">Full management: users, departments, equipment, tickets, and workflow control.</div>
            </div>
            <div class="list-item">
              <div class="item-title">Manager</div>
              <div class="item-meta">Department scope, assignment, and close/cancel permissions for tickets in the same department.</div>
            </div>
            <div class="list-item">
              <div class="item-title">Technician</div>
              <div class="item-meta">Can work on assigned tickets and push status through the execution flow.</div>
            </div>
            <div class="list-item">
              <div class="item-title">Staff</div>
              <div class="item-meta">Can create tickets and follow the progress for their own requests.</div>
            </div>
          </div>
        </div>
      </div>
    `;
  };

  const renderApiView = () => {
    return `
      <div class="view-shell">
        <div class="panel">
          <div class="eyebrow">API map</div>
          <h2 class="section-title" style="margin-top: 12px;">Backend surface area</h2>
          <p class="section-subtitle">This screen is intentionally split by module so the README and the future React app can be read from the same mental model.</p>
        </div>

        <div class="grid-2">
          ${seed.apiGroups.map((group) => `
            <div class="panel">
              <div class="eyebrow">${escapeHtml(group.title)}</div>
              <div class="list" style="margin-top: 14px;">
                ${group.routes.map((route) => `
                  <div class="list-item">
                    <div class="item-top">
                      <div class="item-title">${escapeHtml(route.path)}</div>
                      <span class="${route.method === "GET" ? "chip good" : route.method === "POST" ? "chip warn" : "chip primary"}">${escapeHtml(route.method)}</span>
                    </div>
                    <div class="item-meta">${escapeHtml(route.note)}</div>
                  </div>
                `).join("")}
              </div>
            </div>
          `).join("")}
        </div>
      </div>
    `;
  };

  const resetDepartmentForm = () => {
    state.departmentEditId = null;
    els.departmentId.value = "";
    els.departmentName.value = "";
    els.departmentDescription.value = "";
  };

  const resetEquipmentForm = () => {
    state.equipmentEditId = null;
    els.equipmentId.value = "";
    els.equipmentCode.value = "";
    els.equipmentName.value = "";
    els.equipmentDepartment.value = state.departments[0]?.id ?? "";
    els.equipmentStatus.value = "Active";
    els.equipmentPurchased.value = "";
    els.equipmentDescription.value = "";
  };

  const renderShellFields = () => {
    if (state.view !== "tickets") {
      return;
    }
    const ticket = selectedTicket();
    if (!ticket) return;
    const technician = els.assignTechnician;
    if (technician) technician.value = ticket.assignedTechnicianId ?? "";
  };

  const bindLoginEvents = () => {
    els.loginForm.addEventListener("submit", (event) => {
      event.preventDefault();
      login(els.loginEmail.value.trim(), els.loginPassword.value);
    });

    els.quickLogins.addEventListener("click", (event) => {
      const button = event.target.closest("[data-quick-login]");
      if (!button) return;
      const email = button.getAttribute("data-quick-login");
      const quick = seed.quickLogins.find((item) => item.email === email);
      if (!quick) return;
      els.loginEmail.value = quick.email;
      els.loginPassword.value = quick.password;
      login(quick.email, quick.password);
    });
  };

  const bindShellEvents = () => {
    if (state.view === "tickets") {
      const search = $("ticket-search");
      const status = $("ticket-status");
      const priority = $("ticket-priority");
      const assignBtn = $("assign-btn");
      const progressBtn = $("set-progress-btn");
      const resolveBtn = $("resolve-btn");
      const closeBtn = $("close-btn");
      const cancelBtn = $("cancel-btn");
      const commentBtn = $("comment-btn");

      search?.addEventListener("input", (event) => {
        state.search = event.target.value;
        renderAll();
      });
      status?.addEventListener("change", (event) => {
        state.ticketStatus = event.target.value;
        renderAll();
      });
      priority?.addEventListener("change", (event) => {
        state.ticketPriority = event.target.value;
        renderAll();
      });
      els.primary.querySelectorAll("[data-ticket-select]").forEach((button) => {
        button.addEventListener("click", () => {
          state.selectedTicketId = Number(button.getAttribute("data-ticket-select"));
          renderAll();
        });
      });

      assignBtn?.addEventListener("click", () => {
        const ticket = selectedTicket();
        const techId = Number(els.assignTechnician?.value || 0);
        const note = els.assignNote?.value.trim() || "Assigned from wireframe.";
        if (!ticket) return;
        if (!techId) {
          setBanner("error", "Hãy chọn technician.");
          return;
        }
        const technician = state.users.find((user) => user.id === techId && user.role === "Technician");
        if (!technician) {
          setBanner("error", "User được chọn phải là technician.");
          return;
        }
        ticket.assignedTechnicianId = technician.id;
        ticket.assignedTechnicianName = technician.fullName;
        ticket.status = "Assigned";
        ticket.history.push({
          id: ticket.history.length + 1,
          status: "Assigned",
          note,
          changedBy: getUser()?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        });
        setBanner("success", `Đã assign ${ticket.ticketCode} cho ${technician.fullName}.`);
        renderAll();
      });

      progressBtn?.addEventListener("click", () => {
        const ticket = selectedTicket();
        if (!ticket) return;
        ticket.status = "InProgress";
        ticket.history.push({
          id: ticket.history.length + 1,
          status: "InProgress",
          note: "Technician started work.",
          changedBy: getUser()?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        });
        setBanner("success", `${ticket.ticketCode} moved to In Progress.`);
        renderAll();
      });

      resolveBtn?.addEventListener("click", () => {
        const ticket = selectedTicket();
        if (!ticket) return;
        const resolution = els.resolutionNote?.value.trim();
        if (!resolution) {
          setBanner("error", "Resolution note là bắt buộc khi resolve.");
          return;
        }
        ticket.status = "Resolved";
        ticket.resolutionNote = resolution;
        ticket.resolvedAt = new Date().toISOString();
        ticket.history.push({
          id: ticket.history.length + 1,
          status: "Resolved",
          note: resolution,
          changedBy: getUser()?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        });
        setBanner("success", `${ticket.ticketCode} đã resolve.`);
        renderAll();
      });

      closeBtn?.addEventListener("click", () => {
        const ticket = selectedTicket();
        if (!ticket) return;
        ticket.status = "Closed";
        ticket.closedAt = new Date().toISOString();
        ticket.history.push({
          id: ticket.history.length + 1,
          status: "Closed",
          note: "Ticket closed from wireframe.",
          changedBy: getUser()?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        });
        setBanner("success", `${ticket.ticketCode} đã đóng.`);
        renderAll();
      });

      cancelBtn?.addEventListener("click", () => {
        const ticket = selectedTicket();
        if (!ticket) return;
        ticket.status = "Cancelled";
        ticket.history.push({
          id: ticket.history.length + 1,
          status: "Cancelled",
          note: "Cancelled from wireframe.",
          changedBy: getUser()?.fullName ?? "System",
          changedAt: new Date().toISOString(),
        });
        setBanner("success", `${ticket.ticketCode} đã cancel.`);
        renderAll();
      });

      commentBtn?.addEventListener("click", () => {
        const ticket = selectedTicket();
        if (!ticket) return;
        const content = els.ticketComment?.value.trim();
        if (!content) {
          setBanner("error", "Comment không được để trống.");
          return;
        }
        ticket.comments.push({
          id: ticket.comments.length + 1,
          userName: getUser()?.fullName ?? "System",
          content,
          createdAt: new Date().toISOString(),
        });
        els.ticketComment.value = "";
        setBanner("success", "Đã thêm comment.");
        renderAll();
      });
    }

    if (state.view === "catalog") {
      els.departmentForm?.addEventListener("submit", (event) => {
        event.preventDefault();
        if (!canManageCatalog()) {
          setBanner("error", "Chỉ Admin mới chỉnh catalog trong wireframe này.");
          return;
        }
        const name = els.departmentName.value.trim();
        const description = els.departmentDescription.value.trim();
        if (!name) {
          setBanner("error", "Department name là bắt buộc.");
          return;
        }
        const duplicate = state.departments.some((item) => item.name.toLowerCase() === name.toLowerCase() && item.id !== state.departmentEditId);
        if (duplicate) {
          setBanner("error", "Tên phòng ban đã tồn tại.");
          return;
        }
        if (state.departmentEditId) {
          const item = state.departments.find((department) => department.id === state.departmentEditId);
          if (item) {
            item.name = name;
            item.description = description;
          }
          setBanner("success", "Đã cập nhật department.");
        } else {
          const id = Math.max(...state.departments.map((item) => item.id)) + 1;
          state.departments.unshift({
            id,
            name,
            description,
            createdAt: new Date().toISOString(),
          });
          setBanner("success", "Đã tạo department mới.");
        }
        resetDepartmentForm();
        renderAll();
      });

      els.departmentReset?.addEventListener("click", () => {
        resetDepartmentForm();
      });

      els.primary.querySelectorAll("[data-department-edit]").forEach((button) => {
        button.addEventListener("click", () => {
          state.departmentEditId = Number(button.getAttribute("data-department-edit"));
          const item = state.departments.find((department) => department.id === state.departmentEditId);
          if (!item) return;
          els.departmentId.value = item.id;
          els.departmentName.value = item.name;
          els.departmentDescription.value = item.description;
        });
      });

      els.primary.querySelectorAll("[data-department-delete]").forEach((button) => {
        button.addEventListener("click", () => {
          const id = Number(button.getAttribute("data-department-delete"));
          const hasUsers = state.users.some((user) => user.departmentId === id);
          const hasEquipment = state.equipment.some((item) => item.departmentId === id);
          if (hasUsers || hasEquipment) {
            setBanner("error", "Không thể xóa department vì còn user hoặc equipment liên quan.");
            return;
          }
          state.departments = state.departments.filter((item) => item.id !== id);
          setBanner("success", "Đã xóa department.");
          renderAll();
        });
      });

      els.equipmentForm?.addEventListener("submit", (event) => {
        event.preventDefault();
        if (!canManageCatalog()) {
          setBanner("error", "Chỉ Admin mới chỉnh equipment trong wireframe này.");
          return;
        }
        const code = els.equipmentCode.value.trim();
        const name = els.equipmentName.value.trim();
        const departmentId = Number(els.equipmentDepartment.value);
        const status = els.equipmentStatus.value;
        const purchasedDate = els.equipmentPurchased.value || null;
        const description = els.equipmentDescription.value.trim();
        if (!code || !name || !departmentId) {
          setBanner("error", "Code, name và department là bắt buộc.");
          return;
        }
        const duplicateCode = state.equipment.some((item) => item.code.toLowerCase() === code.toLowerCase() && item.id !== state.equipmentEditId);
        if (duplicateCode) {
          setBanner("error", "Equipment code đã tồn tại.");
          return;
        }
        if (state.equipmentEditId) {
          const item = state.equipment.find((equipment) => equipment.id === state.equipmentEditId);
          if (item && item.code !== code) {
            setBanner("error", "Equipment code không được thay đổi sau khi tạo.");
            return;
          }
          if (item) {
            item.name = name;
            item.departmentId = departmentId;
            item.departmentName = departmentName(departmentId);
            item.status = status;
            item.purchasedDate = purchasedDate;
            item.description = description;
          }
          setBanner("success", "Đã cập nhật equipment.");
        } else {
          const id = Math.max(...state.equipment.map((item) => item.id)) + 1;
          state.equipment.unshift({
            id,
            code,
            name,
            departmentId,
            departmentName: departmentName(departmentId),
            status,
            purchasedDate,
            description,
          });
          setBanner("success", "Đã tạo equipment mới.");
        }
        resetEquipmentForm();
        renderAll();
      });

      els.equipmentReset?.addEventListener("click", () => {
        resetEquipmentForm();
      });

      els.primary.querySelectorAll("[data-equipment-edit]").forEach((button) => {
        button.addEventListener("click", () => {
          state.equipmentEditId = Number(button.getAttribute("data-equipment-edit"));
          const item = state.equipment.find((equipment) => equipment.id === state.equipmentEditId);
          if (!item) return;
          els.equipmentId.value = item.id;
          els.equipmentCode.value = item.code;
          els.equipmentName.value = item.name;
          els.equipmentDepartment.value = item.departmentId;
          els.equipmentStatus.value = item.status;
          els.equipmentPurchased.value = item.purchasedDate ?? "";
          els.equipmentDescription.value = item.description ?? "";
        });
      });

      els.primary.querySelectorAll("[data-equipment-delete]").forEach((button) => {
        button.addEventListener("click", () => {
          const id = Number(button.getAttribute("data-equipment-delete"));
          const hasTicket = state.tickets.some((ticket) => ticket.equipmentId === id);
          if (hasTicket) {
            setBanner("error", "Không thể xóa equipment vì còn ticket liên quan.");
            return;
          }
          state.equipment = state.equipment.filter((item) => item.id !== id);
          setBanner("success", "Đã xóa equipment.");
          renderAll();
        });
      });
    }
  };

  const bindProfileEvents = () => {
    const form = $("change-password-form");
    form?.addEventListener("submit", (event) => {
      event.preventDefault();
      const current = $("current-password").value.trim();
      const next = $("new-password").value.trim();
      const user = getUser();
      if (!user) return;
      if (!current || !next) {
        setBanner("error", "Nhập đủ current password và new password.");
        return;
      }
      if (user.password !== current) {
        setBanner("error", "Current password không đúng.");
        return;
      }
      if (current === next) {
        setBanner("error", "Mật khẩu mới phải khác mật khẩu hiện tại.");
        return;
      }
      user.password = next;
      const quick = seed.quickLogins.find((item) => item.email === user.email);
      if (quick) quick.password = next;
      $("current-password").value = "";
      $("new-password").value = "";
      user.mustChangePassword = false;
      setBanner("success", "Đã đổi mật khẩu. Đăng nhập lại nếu muốn test flow refresh.");
      renderAll();
    });
  };

  const renderAll = () => {
    if (!state.currentUser) {
      return;
    }
    renderTabs();
    renderBanner();
    renderPrimary();
    renderInspector();
    cache();
    renderShellFields();
    bindShellEvents();
    bindProfileEvents();
  };

  const cache = () => {
    els.hero = $("login-screen");
    els.appShell = $("app-shell");
    els.banner = $("app-banner");
    els.loginBanner = $("login-banner");
    els.tabs = $("module-tabs");
    els.logoutBtn = $("logout-btn");
    els.primary = $("primary-panel");
    els.inspector = $("inspector-panel");
    els.quickLogins = $("quick-login-list");
    els.loginForm = $("login-form");
    els.loginEmail = $("login-email");
    els.loginPassword = $("login-password");
    els.assignTechnician = $("assign-technician");
    els.assignNote = $("assign-note");
    els.resolutionNote = $("resolution-note");
    els.ticketComment = $("ticket-comment");
    els.departmentForm = $("department-form");
    els.departmentId = $("department-id");
    els.departmentName = $("department-name");
    els.departmentDescription = $("department-description");
    els.departmentReset = $("department-reset");
    els.equipmentForm = $("equipment-form");
    els.equipmentId = $("equipment-id");
    els.equipmentCode = $("equipment-code");
    els.equipmentName = $("equipment-name");
    els.equipmentDepartment = $("equipment-department");
    els.equipmentStatus = $("equipment-status");
    els.equipmentPurchased = $("equipment-purchased");
    els.equipmentDescription = $("equipment-description");
    els.equipmentReset = $("equipment-reset");
    els.changePasswordForm = $("change-password-form");
  };

  const bindShellControls = () => {
    els.tabs?.addEventListener("click", (event) => {
      const button = event.target.closest("[data-view]");
      if (!button) return;
      state.view = button.getAttribute("data-view");
      renderAll();
    });

    els.logoutBtn?.addEventListener("click", logout);
  };

  const init = () => {
    cache();
    renderQuickLogins();
    seedLoginDefaults();
    toggleShell(false);
    renderBanner();
    bindLoginEvents();
    bindShellControls();
    $("login-screen")?.classList.add("active");
  };

  window.addEventListener("DOMContentLoaded", init);
})();
