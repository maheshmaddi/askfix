namespace AskFix.Api.Services.Email;

/// <summary>Simple branded HTML email bodies (indigo AskFix look).</summary>
public static class EmailTemplates
{
    public static string Notification(string actorName, string action, string questionTitle, string questionUrl, string manageUrl)
    {
        var title = Escape(questionTitle);
        return $"""
        <!doctype html><html><body style="margin:0;padding:0;background:#f4f4f6;font-family:'Segoe UI',Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:28px 12px;">
            <tr><td align="center">
              <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">
                <tr><td style="padding-bottom:18px;">
                  <span style="display:inline-block;width:34px;height:34px;border-radius:9px;background:linear-gradient(135deg,#3F42BD,#7C4FD8);color:#fff;font-weight:800;font-size:19px;text-align:center;line-height:34px;">A</span>
                  <span style="font-size:19px;font-weight:800;color:#191919;margin-left:8px;">AskFix</span>
                </td></tr>
                <tr><td style="background:#ffffff;border-radius:12px;border:1px solid #e3e3e5;padding:26px 28px;">
                  <p style="margin:0 0 14px;font-size:15px;color:#191919;line-height:1.55;">
                    <strong>{Escape(actorName)}</strong> <span style="color:#636466;">{Escape(action)}</span>
                  </p>
                  <p style="margin:0 0 20px;">
                    <a href="{questionUrl}" style="display:inline-block;font-size:16px;font-weight:700;color:#5457D6;text-decoration:none;border-left:3px solid #5457D6;padding-left:12px;line-height:1.4;">{title}</a>
                  </p>
                  <p style="margin:0;">
                    <a href="{questionUrl}" style="display:inline-block;background:#5457D6;color:#ffffff;text-decoration:none;font-size:14px;font-weight:600;border-radius:999px;padding:10px 22px;">Open in AskFix</a>
                  </p>
                </td></tr>
                <tr><td style="padding-top:16px;text-align:center;font-size:12px;color:#939598;line-height:1.6;">
                  You received this because of your AskFix notification settings.<br>
                  <a href="{manageUrl}" style="color:#939598;">Manage notifications</a> · internal use only
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body></html>
        """;
    }

    public static string Test(string baseUrl)
    {
        return $"""
        <!doctype html><html><body style="margin:0;padding:0;background:#f4f4f6;font-family:'Segoe UI',Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="padding:28px 12px;">
            <tr><td align="center">
              <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">
                <tr><td style="background:#ffffff;border-radius:12px;border:1px solid #e3e3e5;padding:28px;">
                  <p style="margin:0 0 10px;font-size:17px;font-weight:800;color:#191919;">✓ AskFix email is working</p>
                  <p style="margin:0 0 16px;font-size:14.5px;color:#636466;line-height:1.6;">
                    This is a test message sent from the AskFix admin panel. Notifications about answers, comments and accepted fixes will look like this.
                  </p>
                  <a href="{baseUrl}" style="display:inline-block;background:#5457D6;color:#ffffff;text-decoration:none;font-size:14px;font-weight:600;border-radius:999px;padding:10px 22px;">Open AskFix</a>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body></html>
        """;
    }

    private static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
