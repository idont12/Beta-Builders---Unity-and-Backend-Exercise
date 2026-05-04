using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGames.Task
{
    /// <summary>
    /// Wires UI for JWT login and local logout (with optional confirmation popup).
    /// Requires <see cref="TaskJwtAuthClient"/> on the same GameObject or referenced explicitly.
    /// </summary>
    public class TaskLoginLogoutUI : MonoBehaviour
    {
        [Header("API")]
        [SerializeField] TaskJwtAuthClient authClient;

        [Header("Login UI")]
        [SerializeField] GameObject loginPanel;
        [SerializeField] TMP_InputField usernameInput;
        [SerializeField] TMP_InputField passwordInput;
        [SerializeField] Button loginButton;
        [SerializeField] TMP_Text loginErrorText;

        [Header("Registration UI (optional)")]
        [SerializeField] GameObject registerPanel;
        [SerializeField] TMP_InputField registerUsernameInput;
        [SerializeField] TMP_InputField registerPasswordInput;
        [SerializeField] Button registerSubmitButton;
        [SerializeField] TMP_Text registerErrorText;
        [Tooltip("From login panel — switch to register screen.")]
        [SerializeField] Button goToRegisterButton;
        [Tooltip("From register panel — back to login.")]
        [SerializeField] Button goToLoginButton;

        [Header("After login")]
        [SerializeField] GameObject loggedInPanel;

        [Header("Logout")]
        [SerializeField] Button logoutButton;
        [SerializeField] GameObject logoutConfirmPanel;
        [SerializeField] Button logoutConfirmYes;
        [SerializeField] Button logoutConfirmNo;

        [Header("Behaviour")]
        [Tooltip("If true, refreshes access token when it is close to expiring (needs refresh token).")]
        [SerializeField] bool proactiveRefresh = true;
        [SerializeField] float refreshWhenLessThanSeconds = 120f;
        [SerializeField] float proactiveCheckIntervalSeconds = 30f;

        float _nextProactiveCheck;
        Coroutine _pendingPlayDefaultGame;

        void Awake()
        {
            if (authClient == null) authClient = GetComponent<TaskJwtAuthClient>();

            if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
            if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);
            if (logoutConfirmYes != null) logoutConfirmYes.onClick.AddListener(OnLogoutConfirmed);
            if (logoutConfirmNo != null) logoutConfirmNo.onClick.AddListener(OnLogoutCancelled);

            if (goToRegisterButton != null) goToRegisterButton.onClick.AddListener(ShowRegisterScreen);
            if (goToLoginButton != null) goToLoginButton.onClick.AddListener(ShowLoginScreen);
            if (registerSubmitButton != null) registerSubmitButton.onClick.AddListener(OnRegisterClicked);

            if (loginErrorText != null) loginErrorText.text = string.Empty;
            if (registerErrorText != null) registerErrorText.text = string.Empty;
            if (logoutConfirmPanel != null) logoutConfirmPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
        }

        void Start()
        {
            var loggedIn = TaskAuthSession.HasRefreshToken;
            ApplyLoggedInState(loggedIn);
            if (loggedIn)
            {
                TaskBackendEvents.RaiseLoggedIn();
                RequestStartDefaultGameNextFrame();
            }
        }

        void Update()
        {
            if (!proactiveRefresh || !TaskAuthSession.HasRefreshToken) return;
            if (authClient == null) return;
            if (Time.unscaledTime < _nextProactiveCheck) return;
            _nextProactiveCheck = Time.unscaledTime + proactiveCheckIntervalSeconds;

            var exp = TaskAuthSession.GetAccessExpiryUtc();
            if (!exp.HasValue) return;

            var secondsLeft = (float)(exp.Value - DateTime.UtcNow).TotalSeconds;
            if (secondsLeft > refreshWhenLessThanSeconds) return;

            authClient.RefreshIfPossible((ok, err) =>
            {
                if (!ok && loginErrorText != null)
                    loginErrorText.text = $"Session refresh failed: {err}";
            });
        }

        void OnLoginClicked()
        {
            if (authClient == null)
            {
                if (loginErrorText != null) loginErrorText.text = "Missing TaskJwtAuthClient reference.";
                return;
            }

            var user = usernameInput != null ? usernameInput.text : string.Empty;
            var pass = passwordInput != null ? passwordInput.text : string.Empty;

            if (loginErrorText != null) loginErrorText.text = string.Empty;
            SetInteractable(loginButton, false);

            authClient.Login(user, pass, (ok, err) =>
            {
                SetInteractable(loginButton, true);
                if (!ok)
                {
                    if (loginErrorText != null) loginErrorText.text = string.IsNullOrEmpty(err) ? "Login failed." : err;
                    return;
                }

                if (passwordInput != null) passwordInput.text = string.Empty;
                ApplyLoggedInState(true);
                TaskBackendEvents.RaiseLoggedIn();
                RequestStartDefaultGameNextFrame();
            });
        }

        void OnRegisterClicked()
        {
            if (authClient == null)
            {
                if (registerErrorText != null)
                    registerErrorText.text = "Missing TaskJwtAuthClient reference.";
                return;
            }

            var user = registerUsernameInput != null ? registerUsernameInput.text : string.Empty;
            var pass = registerPasswordInput != null ? registerPasswordInput.text : string.Empty;

            if (registerErrorText != null) registerErrorText.text = string.Empty;
            SetInteractable(registerSubmitButton, false);

            authClient.Register(user, pass, (regOk, regErr) =>
            {
                if (!regOk)
                {
                    SetInteractable(registerSubmitButton, true);
                    if (registerErrorText != null)
                        registerErrorText.text = string.IsNullOrEmpty(regErr) ? "Registration failed." : regErr;
                    return;
                }

                authClient.Login(user, pass, (loginOk, loginErr) =>
                {
                    SetInteractable(registerSubmitButton, true);
                    if (!loginOk)
                    {
                        if (registerErrorText != null)
                            registerErrorText.text = string.IsNullOrEmpty(loginErr)
                                ? "Account created but login failed."
                                : loginErr;
                        return;
                    }

                    if (registerPasswordInput != null) registerPasswordInput.text = string.Empty;
                    ApplyLoggedInState(true);
                    TaskBackendEvents.RaiseLoggedIn();
                    RequestStartDefaultGameNextFrame();
                });
            });
        }

        void OnLogoutClicked()
        {
            if (logoutConfirmPanel != null)
            {
                logoutConfirmPanel.SetActive(true);
                return;
            }

            OnLogoutConfirmed();
        }

        void OnLogoutCancelled()
        {
            if (logoutConfirmPanel != null) logoutConfirmPanel.SetActive(false);
        }

        void OnLogoutConfirmed()
        {
            TaskAuthSession.Clear();
            TaskBackendEvents.RaiseLoggedOut();
            if (logoutConfirmPanel != null) logoutConfirmPanel.SetActive(false);
            ApplyLoggedInState(false);
        }

        public void ShowLoginScreen()
        {
            if (loginPanel != null) loginPanel.SetActive(true);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (loginErrorText != null) loginErrorText.text = string.Empty;
        }

        public void ShowRegisterScreen()
        {
            if (loginPanel != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(true);
            if (registerErrorText != null) registerErrorText.text = string.Empty;
        }

        void ApplyLoggedInState(bool loggedIn)
        {
            if (loggedInPanel != null) loggedInPanel.SetActive(loggedIn);

            if (loggedIn)
            {
                if (loginPanel != null) loginPanel.SetActive(false);
                if (registerPanel != null) registerPanel.SetActive(false);
                return;
            }

            ShowLoginScreen();
        }

        static void SetInteractable(Selectable s, bool on)
        {
            if (s != null) s.interactable = on;
        }

        /// <summary>
        /// Defer one frame so <see cref="GameManager"/> (and other listeners) have subscribed in <c>OnEnable</c>.
        /// </summary>
        void RequestStartDefaultGameNextFrame()
        {
            if (_pendingPlayDefaultGame != null)
                StopCoroutine(_pendingPlayDefaultGame);
            _pendingPlayDefaultGame = StartCoroutine(PlayDefaultGameAfterOneFrame());
        }

        IEnumerator PlayDefaultGameAfterOneFrame()
        {
            yield return null;
            TaskBackendEvents.RaisePlayDefaultGameRequested();
            _pendingPlayDefaultGame = null;
        }
    }
}
