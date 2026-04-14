using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class MainMenuDestinationLoader : MonoBehaviour
{
    [SerializeField] private RectTransform parent;
    [SerializeField] private DestinationButtonHandler destinationButtonPrefab;

    #region deprecated
    [SerializeField] private bool IsTesting;
    [SerializeField] private DestinationSO AgmonHula;
    [SerializeField] private DestinationSO Shamir;
    [SerializeField] private DestinationSO Gilboa;
    [SerializeField] private DestinationSO Agre;
    [SerializeField] private DestinationSO Golan;
    [SerializeField] private DestinationSO BioCastle;
    [SerializeField] private DestinationSO Salmon;
    [SerializeField] private DestinationSO Eshkol;
    [SerializeField] private DestinationSO Ginosar;
    [SerializeField] private DestinationSO Tzemah;
    [SerializeField] private DestinationSO Afimilk;

    [SerializeField] private InterestPointSO Firewave;
    [SerializeField] private InterestPointSO Seymour;
    [SerializeField] private InterestPointSO Shvitz;
    [SerializeField] private InterestPointSO Tzipori;
    #endregion

    private List<DestinationButtonHandler> activeDestinationButtonList = new List<DestinationButtonHandler>();
    private float onDestinationChangedTransitionTime = 0.8f;
    private Coroutine onDestinationChanged;
    private WaitForSeconds waitForSeconds;
    private bool canChangeDestination;
    private bool _firstInputBlock;
    private int indicator = 0;

    void Awake()
    {
        if (IsTesting)
        {
            //LocationsManager.AddDestination(9, AgmonHula);
            //LocationsManager.AddDestination(1, Shamir);
            //LocationsManager.AddDestination(2, Gilboa);
            //LocationsManager.AddDestination(3, Agre);
            //LocationsManager.AddDestination(4, Golan);
            //LocationsManager.AddDestination(5, BioCastle);
            //LocationsManager.AddDestination(6, Salmon);
            //LocationsManager.AddDestination(7, Eshkol);
            //LocationsManager.AddDestination(8, Ginosar);
            //LocationsManager.AddDestination(9, Tzemah);
            //LocationsManager.AddDestination(10, Afimilk);
            //LocationsManager.AddInterestPoint(0, Firewave);
            //LocationsManager.AddInterestPoint(1, Seymour);
            //LocationsManager.AddInterestPoint(2, Shvitz);
            //LocationsManager.AddInterestPoint(3, Tzipori);
        }

        for (int i = 0; i < LocationsManager.Destinations.Count; i++)
        {
            DestinationButtonHandler handler = Instantiate(destinationButtonPrefab, parent);
            activeDestinationButtonList.Add(handler);
            handler.LoadDestination(i);
        }

        canChangeDestination = true;
        indicator = (activeDestinationButtonList.Count - 1) / 2;
        waitForSeconds = new WaitForSeconds(onDestinationChangedTransitionTime);
    }
    private void OnEnable()
    {
        IdleManager.OnAnyJoystickInput += TryHandleJoytickInput;
        IdleManager.OnAnyJoystickClick += TryHandleJoystickClick;
    }
    private void OnDisable()
    {
        IdleManager.OnAnyJoystickInput -= TryHandleJoytickInput;
        IdleManager.OnAnyJoystickClick -= TryHandleJoystickClick;
    }

    public void TryHandleJoytickInput(int inputChangeValue)
    {
        if (MainMenuManager.IsDestinationSelectionActive)
            ChangeDestination(inputChangeValue);
    }
    public void TryHandleJoystickClick()
    {
        if (UserInterfaceNavigator.GetActiveButton() == null && _firstInputBlock)
            activeDestinationButtonList[indicator].OnClick();

        if (!_firstInputBlock)
            _firstInputBlock = true;
    }

    private void ChangeDestination(int inputChangeValue)
    {
        if (canChangeDestination)
        {
            canChangeDestination = false;
            onDestinationChanged = StartCoroutine(OnDestinationChanged(inputChangeValue));
        }
    }

    /// <summary>
    /// Value is additivly changing the current destination index.
    /// </summary>
    /// <param name="value"></param>
    public void NextDestination(int value)
    {
        NextIndicator(value);
        activeDestinationButtonList[indicator].PlayHoverAnimation(true, activeDestinationButtonList[indicator].GetIsSelected());
    }

    private void NextIndicator(int value)
    {
        indicator += value;

        if (indicator > activeDestinationButtonList.Count - 1)
            indicator = 0;
        else if (indicator < 0)
            indicator = activeDestinationButtonList.Count - 1;
    }
    private void DeselectHoveredDestination()
    {
        activeDestinationButtonList[indicator].PlayHoverAnimation(false, activeDestinationButtonList[indicator].GetIsSelected());
    }

    private IEnumerator OnDestinationChanged(int inputChangeValue)
    {
        DeselectHoveredDestination();
        yield return waitForSeconds;
        NextDestination(inputChangeValue);
        yield return waitForSeconds;
        canChangeDestination = true;
    }

}