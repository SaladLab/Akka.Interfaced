using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections;
using System.Net;
using SlimUnityChat.Interface;
using System.Globalization;

public class LoginDialog : UiDialog
{
    public InputField ServerInput;
    public InputField IdInput;
    public InputField PasswordInput;
    public Text MessageText;

    private bool _isLoginBusy;

    public void Start()
    {
        var loginServer = PlayerPrefs.GetString("LoginServer", "127.0.0.1:9001");
        var loginId = PlayerPrefs.GetString("LoginId", "test");
        var loginPassword = PlayerPrefs.GetString("LoginPassword", "1234");

        ServerInput.text = loginServer;
        IdInput.text = loginId;
        PasswordInput.text = loginPassword;

        SetMessage(null);
    }

    public void OnLoginButtonClick()
    {
        if (_isLoginBusy)
            return;

        _isLoginBusy = true;
        SetMessage(null);

        PlayerPrefs.SetString("LoginServer", ServerInput.text);
        PlayerPrefs.SetString("LoginId", IdInput.text);
        PlayerPrefs.SetString("LoginPassword", PasswordInput.text);

        StartCoroutine(ProcessLogin(ServerInput.text, IdInput.text, PasswordInput.text));
    }

    private IEnumerator ProcessLogin(string server, string id, string password)
    {
        try
        {
            IPEndPoint serverEndPoint;
            try
            {
                serverEndPoint = CreateIPEndPoint(server);
            }
            catch (Exception e)
            {
                SetMessage(e.ToString());
                yield break;
            }

            G.Comm = new Communicator(G.Logger, ApplicationComponent.Instance);
            G.Comm.ServerEndPoint = serverEndPoint;
            G.Comm.Start();

            // Try Login

            var userLogin = new UserLoginRef(new SlimActorRef { Id = 1 }, new SlimRequestWaiter { Communicator = G.Comm }, null);

            var observerId = G.Comm.IssueObserverId();
            var t1 = userLogin.Login(id, password, observerId);
            yield return t1.WaitHandle;
            if (t1.Exception != null)
            {
                var re = t1.Exception as ResultException;
                if (re != null)
                {
                    SetMessage(re.ResultCode.ToString());
                }
                else
                {
                    SetMessage(t1.Exception.ToString());
                }
                yield break;
            }

            G.User = new UserRef(new SlimActorRef { Id = t1.Result }, new SlimRequestWaiter { Communicator = G.Comm }, null);
            G.UserId = id;
            Hide(Tuple.Create(id, observerId));
        }
        finally
        {
            _isLoginBusy = false;
        }
    }

    private void SetMessage(string message)
    {
        TweenHelper.KillAllTweensOfObject(MessageText);

        if (string.IsNullOrEmpty(message))
        {
            MessageText.text = "";
        }
        else
        {
            MessageText.text = message;
            MessageText.DOFade(1f, 0.5f);
            MessageText.DOFade(0f, 0.5f).SetDelay(5);
        }
    }

    // http://stackoverflow.com/questions/2727609/best-way-to-create-ipendpoint-from-string
    private static IPEndPoint CreateIPEndPoint(string endPoint)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
        IPAddress ip;
        if (ep.Length > 2)
        {
            if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
        }
        else
        {
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
        }
        int port;
        if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
        {
            throw new FormatException("Invalid port");
        }
        return new IPEndPoint(ip, port);
    }
}
