/* AcuPay front end: plain jQuery against the AcuPay API. */
const API_BASE = "http://localhost:5080";   // change if your API runs on another port
const TOKEN_KEY = "acupay_token";
const STATUSES = ["Pending", "Matched", "Exception", "Approved", "Rejected"];
const PAGE_SIZE_FALLBACK = 10;               // only used to compute the page count if the API omits pageSize

const queue = { page: 1, totalPages: 1 };
let currentInvoice = null;

/* ---------- helpers ---------- */
function esc(value) {
  return $("<div>").text(value == null ? "" : String(value)).html();
}
function money(n) {
  return n == null ? "-" : Number(n).toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function badge(status) {
  return '<span class="badge badge-' + esc(status) + '">' + esc(status) + "</span>";
}
function getToken() { return sessionStorage.getItem(TOKEN_KEY); }
function parseJwt(token) {
  try {
    const part = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    return JSON.parse(decodeURIComponent(escape(atob(part))));
  } catch (e) { return null; }
}
function session() {
  const token = getToken();
  if (!token) return null;
  const claims = parseJwt(token);
  if (!claims || (claims.exp && claims.exp * 1000 < Date.now())) return null;
  return { username: claims.name, role: claims.role };
}

function showBanner(message, kind) {
  $("#banner").removeClass("error ok").addClass(kind || "error").text(message).prop("hidden", false);
}
function clearBanner() { $("#banner").prop("hidden", true).text(""); }

function api(method, path, body) {
  const headers = {};
  if (getToken()) headers.Authorization = "Bearer " + getToken();
  return $.ajax({
    url: API_BASE + path,
    method: method,
    contentType: "application/json",
    data: body === undefined ? undefined : JSON.stringify(body),
    headers: headers
  });
}

/* Show the API's message field; a 401 on a signed-in session sends the user back to login. */
function handleError(xhr) {
  if (xhr.status === 401 && getToken()) {
    sessionStorage.removeItem(TOKEN_KEY);
    location.hash = "#/login";
    showBanner("Your session has expired. Please sign in again.");
    return;
  }
  const message = xhr.responseJSON && xhr.responseJSON.error && xhr.responseJSON.error.message;
  showBanner(message || "Could not reach the server. Check that the API is running.");
}

function applyRoleVisibility() {
  const s = session();
  $("[data-roles]").each(function () {
    const allowed = String($(this).data("roles")).split(",");
    $(this).toggleClass("role-hidden", !(s && allowed.includes(s.role)));
    $(this).prop("hidden", !(s && allowed.includes(s.role)));
  });
}

/* ---------- routing ---------- */
function showView(name) {
  $("main > section").prop("hidden", true);
  $("#view-" + name).prop("hidden", false);
  $("#topbar nav a").removeClass("active").filter('[data-nav="' + name + '"]').addClass("active");
}

function route() {
  const s = session();
  const hash = location.hash || "#/queue";

  if (!s) {
    $("#topbar").prop("hidden", true);
    showView("login");
    return;
  }
  $("#topbar").prop("hidden", false);
  $("#who-label").text(s.username + " (" + s.role + ")");

  if (!hash.startsWith("#/invoice/") && !hash.startsWith("#/dashboard") && !hash.startsWith("#/queue")) {
    location.hash = "#/queue";
    return;
  }

  if (hash.startsWith("#/invoice/")) {
    showView("detail");
    loadDetail(parseInt(hash.split("/")[2], 10));
  } else if (hash.startsWith("#/dashboard")) {
    showView("dashboard");
    loadDashboard();
  } else {
    showView("queue");
    loadQueue();
  }
}

/* ---------- login ---------- */
$("#login-form").on("submit", function (e) {
  e.preventDefault();
  clearBanner();
  api("POST", "/api/auth/login", {
    username: $("#login-username").val(),
    password: $("#login-password").val()
  }).done(function (res) {
    sessionStorage.setItem(TOKEN_KEY, res.token);
    $("#login-password").val("");
    location.hash = "#/queue";
    route();
  }).fail(function (xhr) {
    if (xhr.status === 401) showBanner("Wrong username or password. Check them and try again.");
    else handleError(xhr);
  });
});

$("#btn-logout").on("click", function () {
  sessionStorage.removeItem(TOKEN_KEY);
  clearBanner();
  location.hash = "#/login";
  route();
});

/* ---------- queue ---------- */
STATUSES.forEach(function (s) { $("#f-status").append($("<option>").val(s).text(s)); });

function loadQueue() {
  clearBanner();
  applyRoleVisibility();
  const params = new URLSearchParams({ page: queue.page });
  if ($("#f-status").val()) params.set("status", $("#f-status").val());
  if ($.trim($("#f-vendor").val())) params.set("vendorCode", $.trim($("#f-vendor").val()));

  api("GET", "/api/invoices?" + params.toString()).done(function (res) {
    const $body = $("#queue-table tbody").empty();
    if (!res.items.length) {
      $body.append('<tr><td class="empty" colspan="7">No invoices match these filters.</td></tr>');
    }
    res.items.forEach(function (i) {
      $body.append(
        '<tr data-id="' + i.invoiceId + '">' +
        "<td>" + esc(i.invoiceNumber) + "</td>" +
        "<td>" + esc(i.vendorCode) + " " + esc(i.vendorName) + "</td>" +
        "<td>" + esc(i.poNumber || "none") + "</td>" +
        '<td class="num">' + money(i.invoiceAmount) + "</td>" +
        "<td>" + esc(i.invoiceDate) + "</td>" +
        "<td>" + badge(i.status) + "</td>" +
        "<td>" + esc(i.matchNote || "") + "</td></tr>");
    });
    queue.totalPages = Math.max(1, Math.ceil(res.totalCount / (res.pageSize || PAGE_SIZE_FALLBACK)));
    $("#pg-label").text("Page " + res.page + " of " + queue.totalPages + " (" + res.totalCount + " invoices)");
    $("#pg-prev").prop("disabled", res.page <= 1);
    $("#pg-next").prop("disabled", res.page >= queue.totalPages);
  }).fail(handleError);
}

$("#filter-form").on("submit", function (e) { e.preventDefault(); queue.page = 1; loadQueue(); });
$("#pg-prev").on("click", function () { if (queue.page > 1) { queue.page--; loadQueue(); } });
$("#pg-next").on("click", function () { if (queue.page < queue.totalPages) { queue.page++; loadQueue(); } });
$("#queue-table tbody").on("click", "tr[data-id]", function () { location.hash = "#/invoice/" + $(this).data("id"); });

$("#btn-new").on("click", function () { $("#new-form").prop("hidden", false); });
$("#btn-new-cancel").on("click", function () { $("#new-form").prop("hidden", true); });
$("#new-form").on("submit", function (e) {
  e.preventDefault();
  clearBanner();
  api("POST", "/api/invoices", {
    invoiceNumber: $("#n-number").val(),
    vendorCode: $("#n-vendor").val(),
    poNumber: $("#n-po").val() || null,
    invoiceAmount: parseFloat($("#n-amount").val()),
    invoiceDate: $("#n-date").val()
  }).done(function (inv) {
    $("#new-form")[0].reset();
    $("#new-form").prop("hidden", true);
    showBanner("Invoice " + inv.invoiceNumber + " created. Open it and run the match.", "ok");
    loadQueue();
  }).fail(handleError);
});

/* ---------- detail ---------- */
function loadDetail(id) {
  clearBanner();
  api("GET", "/api/invoices/" + id).done(renderDetail).fail(handleError);
}

function renderDetail(inv) {
  currentInvoice = inv;
  const s = session();
  $("#d-title").text(inv.invoiceNumber);
  $("#d-status").attr("class", "badge badge-" + inv.status).text(inv.status);
  $("#d-po").text(money(inv.poAmount));
  $("#d-inv").text(money(inv.invoiceAmount));
  $("#d-var").text(inv.variancePercent == null ? "Not calculated" : inv.variancePercent.toFixed(2) + "%");
  $("#d-note").text(inv.matchNote || "Not matched yet");

  const facts = [
    ["Vendor", inv.vendorCode + " " + inv.vendorName],
    ["Purchase order", inv.poNumber || "None"],
    ["Invoice date", inv.invoiceDate],
    ["Created by", inv.createdBy],
    ["Decided by", inv.decidedBy || "Not decided"],
    ["Decision reason", inv.decisionReason || "-"]
  ];
  $("#d-facts").html(facts.map(function (f) { return "<dt>" + esc(f[0]) + "</dt><dd>" + esc(f[1]) + "</dd>"; }).join(""));

  const decidable = inv.status === "Matched" || inv.status === "Exception";
  const matchable = inv.status !== "Approved" && inv.status !== "Rejected";
  $("#d-reason").val("");
  $("#btn-approve, #btn-reject").prop("hidden", !(decidable && s && ["Approver", "Finance"].includes(s.role)));
  $("#btn-match").prop("hidden", !(matchable && s && ["Requester", "Finance"].includes(s.role)));
  $("#d-actions").prop("hidden", $("#btn-approve").prop("hidden") && $("#btn-reject").prop("hidden") && $("#btn-match").prop("hidden"));
}

function act(path, body, okMessage) {
  clearBanner();
  api("POST", "/api/invoices/" + currentInvoice.invoiceId + "/" + path, body).done(function (inv) {
    renderDetail(inv);
    showBanner(okMessage, "ok");
  }).fail(handleError);
}

$("#btn-match").on("click", function () { act("match", undefined, "Match complete."); });
$("#btn-approve").on("click", function () { act("approve", { reason: $("#d-reason").val() }, "Invoice approved."); });
$("#btn-reject").on("click", function () { act("reject", { reason: $("#d-reason").val() }, "Invoice rejected."); });

/* ---------- dashboard ---------- */
function loadDashboard() {
  clearBanner();
  const $counts = $("#status-counts").empty();
  STATUSES.forEach(function (status) {
    const $card = $('<div class="count">').append($('<span class="n">').text("-"), badge(status));
    $counts.append($card);
    api("GET", "/api/invoices?status=" + status + "&page=1").done(function (res) {
      $card.find(".n").text(res.totalCount);
    }).fail(handleError);
  });

  const $chart = $("#spend-chart").empty();
  const s = session();
  if (!s || s.role !== "Finance") {
    $chart.append('<p class="muted">Spend by vendor is visible to the Finance role only.</p>');
    return;
  }
  api("GET", "/api/reports/spend-by-vendor").done(function (rows) {
    if (!rows.length) { $chart.append('<p class="muted">No approved invoices yet.</p>'); return; }
    const max = Math.max.apply(null, rows.map(function (r) { return r.totalSpend; }));
    rows.forEach(function (r) {
      const pct = max > 0 ? (r.totalSpend / max) * 100 : 0;
      $chart.append(
        '<div class="bar-row"><span>' + esc(r.vendorCode) + " " + esc(r.vendorName) + "</span>" +
        '<div class="bar-track"><div class="bar" style="width:' + pct.toFixed(1) + '%"></div></div>' +
        '<span class="amt">' + money(r.totalSpend) + "</span></div>");
    });
  }).fail(handleError);
}

/* ---------- start ---------- */
$(window).on("hashchange", route);
$(route);
