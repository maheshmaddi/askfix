using AskFix.Api.Models;

namespace AskFix.Api.Data;

/// <summary>Sample content for first run: demo users, common tool tags and realistic Q&A.</summary>
public static class SeedData
{
    private const int DaysAgo = -1; // helper for readable timestamps

    public static void Seed(AppDbContext db)
    {
        var users = new[]
        {
            NewUser("corp\\mahesh",  "Mahesh Patil",  "mahesh.patil@corp.example",   "Developer",           "Full-stack developer. TypeScript, C#, coffee.", 210, hue: 0),
            NewUser("corp\\priya.s", "Priya Sharma",  "priya.sharma@corp.example",   "IT Support",          "Level 2 IT support. VPN, Outlook, laptops — ask me anything.", 840, hue: 265, isAdmin: true),
            NewUser("corp\\rahul.v", "Rahul Verma",   "rahul.verma@corp.example",    "Developer",           "Backend dev. Node.js and reluctant Java.", 350, hue: 205),
            NewUser("corp\\meera.i", "Meera Iyer",    "meera.iyer@corp.example",     "DevOps Engineer",     "I keep the build farm alive. CI/CD, Docker, Jenkins.", 620, hue: 150),
            NewUser("corp\\arjun.p", "Arjun Patel",   "arjun.patel@corp.example",    "QA Engineer",         "Breaking things professionally so you don't have to.", 180, hue: 30),
            NewUser("corp\\sneha.r", "Sneha Reddy",   "sneha.reddy@corp.example",    "Engineering Manager", "Manager, Platform team. I ask questions so my team doesn't have to.", 60, hue: 320),
        };
        db.Users.AddRange(users);
        db.SaveChanges();

        var tags = new Dictionary<string, Tag>();
        foreach (var (name, color, desc) in new[]
        {
            ("VPN",       "#0ea5e9", "Cisco AnyConnect and remote access"),
            ("Proxy",     "#f59e0b", "Corporate proxy, SSL inspection, certificates"),
            ("Git",       "#ef4444", "Git, GitLab, version control"),
            ("Jenkins",   "#d97706", "Build server, pipelines, agents"),
            ("Outlook",   "#6366f1", "Email, calendar, Exchange"),
            ("VS Code",   "#8b5cf6", "Editor setup and extensions"),
            ("Node.js",   "#16a34a", "Node, npm, yarn, pnpm"),
            ("Windows",   "#0891b2", "Windows 10/11, policies, imaging"),
            ("Docker",    "#2493ed", "Containers on the build machines"),
            ("Access",    "#db2777", "Permissions, software requests, approvals"),
            ("SharePoint","#0369a1", "Document libraries and check-out"),
            ("Python",    "#eab308", "Python and pip setup"),
        })
        {
            var tag = new Tag { Name = name, Slug = name.ToLowerInvariant().Replace(".", ""), Color = color, Description = desc };
            tags[name] = tag;
            db.Tags.Add(tag);
        }
        db.SaveChanges();

        // ---- Questions, answers, votes & comments -------------------------------------------
        Q(db, users, tags, days: 21, views: 342, author: 1, title: "npm install fails with ETIMEDOUT behind the corporate proxy — how do I configure npm correctly?",
            tags: ["Node.js", "Proxy"], body: """
            <p>Since the new proxy rollout <code>npm install</code> dies after a couple of minutes with <code>ETIMEDOUT</code>. Our team can't install anything.</p>
            <pre><code class="language-bash">npm ERR! network request to https://registry.npmjs.org/express failed, reason: ETIMEDOUT</code></pre>
            <p>I already set HTTP_PROXY / HTTPS_PROXY in my shell. What am I missing?</p>
            """,
            answers: [
                A(users[0], 24, accepted: true, body: """
                <p>NPM ignores the environment variables in some contexts (especially from VS Code tasks). Configure it directly:</p>
                <pre><code class="language-bash">npm config set proxy http://proxy.corp.example:8080
                npm config set https-proxy http://proxy.corp.example:8080
                npm config set registry https://registry.npmjs.org/</code></pre>
                <p>Then verify with <code>npm config list</code>. Restart your terminal afterwards — VS Code's integrated terminal caches the old env.</p>
                <p>If it still times out, the proxy cert bundle is probably missing. See the registry SSL answer on this site.</p>
                """, comments: ["Finally. Two days lost to this. Restarting the terminal was the missing piece for me.", "Works. Also worth adding strict-ssl=false is NOT the fix, don't do it."]),
                A(users[2], 6, body: """
                <p>In addition to Mahesh's answer: if you use <strong>yarn</strong> or <strong>pnpm</strong> they have their own proxy config, the npm settings don't carry over:</p>
                <pre><code class="language-bash">yarn config set httpProxy http://proxy.corp.example:8080
                pnpm config set proxy http://proxy.corp.example:8080</code></pre>
                """),
            ]);

        Q(db, users, tags, days: 12, views: 517, author: 4, title: "VPN disconnects every 4-5 minutes since the AnyConnect client update — anyone else seeing this?",
            tags: ["VPN", "Windows"], body: """
            <p>After IT pushed the AnyConnect 5.1 update my VPN session drops every 4-5 minutes and reconnects. Colleagues on the floor below are fine. Windows 11, latest patches.</p>
            <p>Log shows <code>Connection attempt has timed out</code> then it reconnects on its own. Driving me crazy during test runs.</p>
            """,
            answers: [
                A(users[1], 38, accepted: true, body: """
                <p>This is a known issue with the 5.1 client + certain Wi-Fi drivers — the network stack resets the tunnel during driver power-save. Two fixes, do both:</p>
                <ol>
                <li>In AnyConnect &rarr; Preferences, disable <strong>"Block connections to untrusted servers"</strong> and enable <strong>"Keep awake during VPN session"</strong>.</li>
                <li>Device Manager &rarr; your Wi-Fi adapter &rarr; Power Management &rarr; uncheck <strong>"Allow the computer to turn off this device"</strong>.</li>
                </ol>
                <p>If it still drops, switch to the wired dock for a day to confirm it's the radio. Ping me on Teams if it persists — I can check your session logs on the gateway side.</p>
                """, comments: ["The power management checkbox fixed it. Why is that on by default on a corporate image 😑", "Confirming the fix works on my X1 Carbon too."]),
                A(users[5], 4, body: """
                <p>Adding a data point: my whole team had this on HP EliteBooks until IT re-pushed the image with updated drivers last week. So it's driver-related, yes.</p>
                """),
            ]);

        Q(db, users, tags, days: 9, views: 288, author: 0, title: "Git push hangs forever after upgrading to Git 2.45 — pull works fine",
            tags: ["Git"], body: """
            <p>After the upgrade, <code>git pull</code> and <code>git fetch</code> work, but <code>git push</code> hangs at <code>Writing objects: 100%</code> and never finishes. Have to Ctrl+C after 10 minutes. Happens on all my repos, small and large.</p>
            """,
            answers: [
                A(users[3], 31, accepted: true, body: """
                <p>Symptom = the HTTP/2 push path through the proxy. The proxy appliance firmware we run mangles large HTTP/2 POST bodies. Force HTTP/1.1 for the GitLab host:</p>
                <pre><code class="language-bash">git config --global http.version HTTP/1.1</code></pre>
                <p>That's the workaround until the proxy firmware update lands next month. This affected ~40 people so far — you're not alone.</p>
                """, comments: ["Instant fix. Thanks Meera!"]),
                A(users[0], 9, body: """
                <p>If you still see hangs on huge pushes (1GB+), also raise the post buffer so it buffers instead of streaming chunked:</p>
                <pre><code class="language-bash">git config --global http.postBuffer 524288000</code></pre>
                """),
            ]);

        Q(db, users, tags, days: 6, views: 173, author: 3, title: "How do I give Jenkins build executors more memory for large npm builds?",
            tags: ["Jenkins", "Node.js"], body: """
            <p>Our frontend monorepo build gets killed with <code>JS heap out of memory</code> on the Jenkins agents. Works on my 32GB laptop, dies on the 8GB agents. Where do I change this — agent JVM or Node?</p>
            """,
            answers: [
                A(users[3], 19, body: """
                <p>Two different heaps, and you probably only need to touch the Node one:</p>
                <p><strong>Node heap</strong> (what's dying in your case):</p>
                <pre><code class="language-bash">export NODE_OPTIONS=--max-old-space-size=4096
                npm run build</code></pre>
                <p>Set it in the pipeline environment block, not on the agent globally, so other jobs aren't affected.</p>
                <p><strong>Agent JVM heap</strong> (only if the agent itself dies): edit <code>jenkins-agent/jenkins-slave.jvmopts</code> or the service <code>--Xmx</code> flag.</p>
                """, comments: ["NODE_OPTIONS in the environment block did it, thanks."]),
            ]);

        Q(db, users, tags, days: 5, views: 402, author: 5, title: "Outlook stuck on \"Trying to connect\" only in the office — fine from home",
            tags: ["Outlook", "Windows"], body: """
            <p>When I'm in the office, Outlook 365 shows <strong>Trying to connect…</strong> for hours. From home everything works instantly. VPN on/off doesn't matter in the office since we're on the corp network directly. Ideas?</p>
            """,
            answers: [
                A(users[1], 27, accepted: true, body: """
                <p>Classic autodiscover-through-proxy issue. Outlook on the corp network resolves autodiscover via the proxy with your Windows session, and cached credentials from the last password change break it.</p>
                <ol>
                <li>Close Outlook.</li>
                <li>Control Panel &rarr; Credential Manager &rarr; Windows Credentials.</li>
                <li>Remove entries starting with <code>MSOffice</code>, <code>MicrosoftOffice16</code> and <code>Outlook</code>.</li>
                <li>Reopen Outlook, sign in once when prompted.</li>
                </ol>
                <p>If it comes back after a password change, just repeat. We're pushing a fix via policy next quarter.</p>
                """, comments: ["Credential Manager cleanup worked. It's the 3rd password change this year so that tracks."]),
            ]);

        Q(db, users, tags, days: 4, views: 156, author: 0, title: "VS Code Remote-SSH can't reach the build server — connection times out",
            tags: ["VS Code"], body: """
            <p><code>ssh build01</code> from a plain terminal connects in 1 second, but VS Code Remote-SSH times out. Same key, same config file. What does VS Code do differently?</p>
            """,
            answers: [
                A(users[2], 16, body: """
                <p>VS Code uses its own ssh invocation and gets confused by multi-hop configs and older ciphers. In the Remote-SSH settings, point it at the exact config file and enable logging:</p>
                <pre><code class="language-json">"remote.SSH.configFile": "C:\\\\Users\\\\you\\\\.ssh\\\\config",
                "remote.SSH.showLoginTerminal": true</code></pre>
                <p>Then check the Remote-SSH output panel. Nine times out of ten it's a <code>ProxyJump</code> line or a <code>ControlMaster</code> socket that the GUI session can't reuse.</p>
                """, comments: ["ProxyJump was it — VS Code didn't like my ControlMaster settings. Removing the Control* lines fixed it."]),
            ]);

        Q(db, users, tags, days: 3, views: 219, author: 3, title: "Docker on the shared build machine: \"no space left on device\" every week",
            tags: ["Docker"], body: """
            <p>Every Monday the shared build VM is out of disk because of dangling Docker layers. Is there a supported cleanup approach so we stop playing whack-a-mole?</p>
            """,
            answers: [
                A(users[3], 29, accepted: true, body: """
                <p>I added a nightly cleanup on all build agents. For a shared machine, prune aggressively — dangling images and stopped containers older than 24h:</p>
                <pre><code class="language-bash">docker system prune -af --filter "until=24h"
                docker volume prune -f --filter "label!=keep"</code></pre>
                <p>Scheduled task runs at 2am via the agent's cron. Also: pull a base image <em>once</em> per pipeline run instead of per stage — our templates were pulling 800MB of node:20 per stage.</p>
                """),
                A(users[4], 7, body: """
                <p>We also hit image-count limits. <code>docker image prune -a --filter until=48h</code> keeps only what the last two days used — nothing older builds anyway (tags are rebuilt fresh).</p>
                """),
            ]);

        Q(db, users, tags, days: 2, views: 97, author: 4, title: "Software install rights: admin account vs IT pre-approval — what's the fast path for dev tools?",
            tags: ["Access", "Windows"], body: """
            <p>My team needs VS Code extensions, Node LTS and Docker Desktop on new laptops. Local admin is locked down. What's the recommended route so we're not filing tickets per machine?</p>
            """,
            answers: [
                A(users[1], 22, accepted: true, body: """
                <p>Fast path is a pre-approval: manager mails IT with the software list + business reason, we add the packages to the <strong>Company Portal</strong> catalog, then anyone self-installs without a ticket. Takes us a day to publish.</p>
                <p>For one-off machines use the Software Request form in the portal instead — SLA is 4h, not the generic 3 days. Never request local admin; that gets auto-rejected by policy.</p>
                """, comments: ["Pre-approval got published in a day. This should be in onboarding docs."]),
            ]);

        Q(db, users, tags, days: 2, views: 134, author: 1, title: "pip install fails with \"certificate verify failed\" — where is the corp root CA bundle?",
            tags: ["Python", "Proxy"], body: """
            <p>On the new laptops <code>pip install requests</code> fails with <code>SSLError: certificate verify failed</code>. The proxy re-signs traffic with the internal CA. Where do I point pip/Python so it trusts it?</p>
            """,
            answers: [
                A(users[2], 14, accepted: true, body: """
                <p>The root CA is at <code>\\\\fileshare\\it\\certs\\CorpRootCA.crt</code>. Two options:</p>
                <p><strong>Per-tool (recommended):</strong></p>
                <pre><code class="language-bash">pip config set global.cert \\\\fileshare\\it\\certs\\CorpRootCA.crt
                # or per-call:
                pip install --cert \\\\fileshare\\it\\certs\\CorpRootCA.crt requests</code></pre>
                <p><strong>System-wide:</strong> use <code>certutil -addstore Root CorpRootCA.crt</code> (admin) — pip on recent versions reads the Windows trust store automatically. Don't use <code>--trusted-host</code>, that disables validation entirely.</p>
                """),
            ]);

        Q(db, users, tags, days: 1, views: 76, author: 5, title: "SharePoint check-out conflicts when two people edit the same spec document",
            tags: ["SharePoint"], body: """
            <p>Our spec docs live on SharePoint. Twice this month two editors got conflicting copies and someone's changes vanished into a "conflict" file nobody noticed for days. Is co-authoring supposed to prevent this?</p>
            """,
            answers: [
                A(users[1], 11, body: """
                <p>Co-authoring only works for <strong>.docx in the browser/desktop Word with AutoSave on</strong>. It breaks when someone opens via File Explorer sync or saves from an old Word version — then you get silent check-outs.</p>
                <p>Practical rules for the team: always open from the browser link (not the synced folder), keep AutoSave on, and turn on <strong>Require documents to be checked out before editing = No</strong> in library settings (IT can do it for you). The conflict files stay in version history — nothing is truly lost, we can restore.</p>
                """, comments: ["Please turn that library setting off for our site, I'll mail you the URL."]),
            ]);

        Q(db, users, tags, days: 1, views: 58, author: 2, title: "nvm breaks PATH after the company image update — node -v shows wrong version per terminal",
            tags: ["Node.js", "Windows"], body: """
            <p>After the laptop re-image, every new terminal gets a different <code>node -v</code>. nvm-windows says 20.11.0 is active but terminals randomly run 18.x from <code>C:\\Program Files\\nodejs</code>.</p>
            """,
            answers: [
                A(users[0], 8, body: """
                <p>The image installs Node 18 system-wide AND nvm — both fight over PATH. nvm's symlink entry must come before <code>Program Files\\nodejs</code>:</p>
                <p>System Properties &rarr; Environment Variables &rarr; move <code>%NVM_HOME%</code> and the <code>nvm_symlink</code> path above the <code>Program Files\\nodejs</code> entry, or delete the latter entirely (it's the 18.x copy). New terminals only — restart VS Code too.</p>
                """),
            ]);

        Q(db, users, tags, days: 0, views: 31, author: 4, title: "Jenkins pipeline can't find Chrome for Playwright UI tests — works locally",
            tags: ["Jenkins"], body: "<p>Our Playwright suite runs green locally but on the Jenkins agent every test fails with <code>Executable doesn't exist at …chrome</code>. How do agents get browsers?</p>",
            answers: []);

        db.SaveChanges();

        // Follows & bookmarks to liven up the demo
        var q1 = db.Questions.OrderBy(q => q.Id).ToList();
        db.QuestionFollows.AddRange(
            new QuestionFollow { UserId = users[0].Id, QuestionId = q1[1].Id },
            new QuestionFollow { UserId = users[4].Id, QuestionId = q1[1].Id },
            new QuestionFollow { UserId = users[5].Id, QuestionId = q1[1].Id },
            new QuestionFollow { UserId = users[2].Id, QuestionId = q1[0].Id });
        db.Bookmarks.AddRange(
            new Bookmark { UserId = users[0].Id, QuestionId = q1[2].Id },
            new Bookmark { UserId = users[0].Id, QuestionId = q1[6].Id });
        db.SaveChanges();
    }

    private static User NewUser(string sam, string name, string email, string dept, string bio, int reputation, int hue, bool isAdmin = false) => new()
    {
        SamAccountName = sam,
        DisplayName = name,
        Email = email,
        Department = dept,
        Bio = bio,
        Reputation = reputation,
        AvatarHue = hue,
        IsAdmin = isAdmin,
        LastLoginAt = DateTime.UtcNow.AddDays(DaysAgo),
    };

    private static (User author, int upvotes, bool accepted, string body, string[] comments) A(
        User author, int upvotes, bool accepted = false, string body = "", string[]? comments = null) =>
        (author, upvotes, accepted, body, comments ?? []);

    private static void Q(AppDbContext db, User[] users, Dictionary<string, Tag> allTags, int days, int views,
        int author, string title, string[] tags, string body, (User author, int upvotes, bool accepted, string body, string[] comments)[] answers)
    {
        var createdAt = DateTime.UtcNow.AddDays(days - 21).AddHours(3);
        var q = new Question
        {
            AuthorId = users[author].Id,
            Title = title,
            BodyHtml = body,
            BodyText = Common.HtmlText.ToText(body),
            ViewCount = views,
            CreatedAt = createdAt,
            LastActivityAt = createdAt,
        };
        foreach (var t in tags)
        {
            var tag = allTags[t];
            q.QuestionTags.Add(new QuestionTag { Tag = tag });
            tag.QuestionCount++;
        }

        var ci = 0;
        foreach (var (aAuthor, upvotes, accepted, aBody, comments) in answers)
        {
            var a = new Answer
            {
                AuthorId = aAuthor.Id,
                BodyHtml = aBody,
                BodyText = Common.HtmlText.ToText(aBody),
                UpvoteCount = upvotes,
                IsAccepted = accepted,
                CreatedAt = createdAt.AddHours(2),
            };
            // distinct voters only (PK is UserId+AnswerId); the headline count stays denormalized
            foreach (var voter in users.Where(u => u.Id != aAuthor.Id).Take(upvotes))
                a.Votes.Add(new AnswerVote { UserId = voter.Id, Value = 1 });
            foreach (var c in comments)
                a.Comments.Add(new Comment { AuthorId = users[(ci++ * 2 + 4) % users.Length].Id, Body = c, CreatedAt = createdAt.AddHours(5) });
            a.CommentCount = comments.Length;
            q.Answers.Add(a);
            q.AnswerCount++;
            q.HasAccepted |= accepted;
        }

        q.LastActivityAt = createdAt.AddHours(6);
        db.Questions.Add(q);
    }
}
