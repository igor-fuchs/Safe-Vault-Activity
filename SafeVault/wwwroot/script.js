let authToken = "";
let currentRole = "";

function parseJwt(token) {
    try {
        const b64 = token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
        return JSON.parse(atob(b64));
    } catch {
        return {};
    }
}

const STATUS_TEXTS = {
    200: "OK",
    201: "Created",
    204: "No Content",
    400: "Bad Request",
    401: "Unauthorized",
    403: "Forbidden",
    404: "Not Found",
    409: "Conflict",
    422: "Unprocessable Entity",
    500: "Internal Server Error",
};

function setResponse(prefix, statusCode, isSuccess, userMsg, rawObj) {
    document.getElementById(prefix + "Response").style.display = "block";
    const msgEl = document.getElementById(prefix + "Msg");
    msgEl.textContent = userMsg;
    msgEl.className = "user-msg " + (isSuccess ? "success" : "error");
    // Status code badge
    const scEl = document.getElementById(prefix + "Sc");
    const scText = document.getElementById(prefix + "ScText");
    if (scEl && scText) {
        const cls =
            statusCode >= 500
                ? "sc sc-5xx"
                : statusCode >= 400
                  ? "sc sc-4xx"
                  : statusCode >= 300
                    ? "sc sc-3xx"
                    : "sc sc-2xx";
        scEl.className = cls;
        scEl.textContent = statusCode;
        scText.textContent = STATUS_TEXTS[statusCode] || "";
    }
    document.getElementById(prefix + "Raw").textContent =
        typeof rawObj === "string" ? rawObj : JSON.stringify(rawObj, null, 2);
}

function updateStatus() {
    const dot = document.getElementById("status-dot");
    const text = document.getElementById("status-text");
    const loggedIn = !!authToken;
    if (!loggedIn) {
        dot.textContent = "\u26AA";
        text.textContent = "Not logged in. Please register or login below.";
    } else {
        dot.textContent = "\uD83D\uDFE2";
        text.innerHTML =
            "Logged in as <strong>" +
            currentRole +
            "</strong> \u2014 JWT token is active and sent automatically in the requests below.";
    }
    // Show/hide login-required notices
    document.getElementById("meNotice").style.display = loggedIn
        ? "none"
        : "block";
    document.getElementById("listNotice").style.display = loggedIn
        ? "none"
        : "block";
}

/* POST /api/user/register */
document
    .getElementById("registerForm")
    .addEventListener("submit", async (e) => {
        e.preventDefault();
        const f = e.target;
        const btn = f.querySelector("button");
        btn.disabled = true;
        try {
            const res = await fetch("/api/user/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    username: f.username.value,
                    email: f.email.value,
                    password: f.password.value,
                }),
            });
            const body = await res.json();
            if (res.ok) {
                setResponse(
                    "register",
                    res.status,
                    true,
                    '\u2705 User "' +
                        body.username +
                        '" registered successfully! Role assigned: ' +
                        body.role,
                    body,
                );
                f.reset();
            } else {
                setResponse(
                    "register",
                    res.status,
                    false,
                    "\u274C Registration failed \u2014 see details below.",
                    body,
                );
            }
        } catch (err) {
            setResponse(
                "register",
                0,
                false,
                "\u274C Network error: " + err.message,
                {},
            );
        } finally {
            btn.disabled = false;
        }
    });

/* POST /api/user/login */
document.getElementById("loginForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = e.target;
    const btn = f.querySelector("button");
    btn.disabled = true;
    try {
        const res = await fetch("/api/user/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                username: f.username.value,
                password: f.password.value,
            }),
        });
        const body = await res.json();
        if (res.ok) {
            authToken = body.token;
            currentRole = body.user.role;
            updateStatus();
            setResponse(
                "login",
                res.status,
                true,
                "\u2705 Login successful! Welcome, " +
                    body.user.username +
                    " (" +
                    currentRole +
                    "). Token stored.",
                { user: body.user, jwtPayload: parseJwt(authToken) },
            );
            f.reset();
        } else {
            setResponse(
                "login",
                res.status,
                false,
                "\u274C Login failed \u2014 invalid credentials.",
                body,
            );
        }
    } catch (err) {
        setResponse(
            "login",
            0,
            false,
            "\u274C Network error: " + err.message,
            {},
        );
    } finally {
        btn.disabled = false;
    }
});

/* GET /api/user/me */
document.getElementById("getMeBtn").addEventListener("click", async () => {
    try {
        const res = await fetch("/api/user/me", {
            headers: { Authorization: "Bearer " + authToken },
        });
        let body;
        try {
            body = await res.json();
        } catch {
            body = { error: res.statusText };
        }
        if (res.ok) {
            setResponse(
                "me",
                res.status,
                true,
                "\u2705 Profile retrieved for: " +
                    body.username +
                    " (Role: " +
                    body.role +
                    ")",
                body,
            );
        } else {
            setResponse(
                "me",
                res.status,
                false,
                "\u274C " +
                    res.status +
                    " " +
                    res.statusText +
                    " \u2014 " +
                    (body.error || "token is missing or expired."),
                body,
            );
        }
    } catch (err) {
        setResponse("me", 0, false, "\u274C Network error: " + err.message, {});
    }
});

/* GET /api/user */
document.getElementById("listUsersBtn").addEventListener("click", async () => {
    document.getElementById("listTable").innerHTML = "";
    try {
        const res = await fetch("/api/user", {
            headers: { Authorization: "Bearer " + authToken },
        });
        let body;
        try {
            body = await res.json();
        } catch {
            body = { error: res.statusText };
        }
        if (res.ok) {
            let table =
                "<table><tr><th>Id</th><th>Username</th><th>Email</th><th>Role</th></tr>";
            body.forEach((u) => {
                table +=
                    "<tr><td>" +
                    u.id +
                    "</td><td>" +
                    u.username +
                    "</td><td>" +
                    u.email +
                    "</td><td>" +
                    u.role +
                    "</td></tr>";
            });
            table += "</table>";
            document.getElementById("listTable").innerHTML = table;
            setResponse(
                "list",
                res.status,
                true,
                "\u2705 " + body.length + " user(s) retrieved.",
                body,
            );
        } else {
            setResponse(
                "list",
                res.status,
                false,
                "\u274C " +
                    res.status +
                    " " +
                    res.statusText +
                    " \u2014 " +
                    (body.error ||
                        "only Admin users can access this endpoint. Login as admin / Admin@1234! and try again."),
                body,
            );
        }
    } catch (err) {
        setResponse(
            "list",
            0,
            false,
            "\u274C Network error: " + err.message,
            {},
        );
    }
});
