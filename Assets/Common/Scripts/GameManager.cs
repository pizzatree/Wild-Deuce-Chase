using System;
using System.Collections;
using System.Collections.Generic;
using Common.Player.Inputs;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private UnityEvent _ohNo;

    [SerializeField] TMP_Text _taxDollarText;
    [SerializeField] GameObject _settingsMenu;
    [SerializeField] float _deductionTime = 10;
    [SerializeField] float _difficultyCurve = 1.25f;

    public HashSet<Bonkable> Bonkables;

    PlayerInputs _inputs;
    int _visitorsSinceLastPath = 10;
    float _visitorsTillNextPath = 1;
    int _taxDollars = 0;
    int _visitorsLeft = 0;
    int _staffCost = 0;
    bool _gameOver = false;

    void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            UpdateText();
            Bonkables             =  new HashSet<Bonkable>();
            Bonkable.OnSpawnEvent += HandleBonkableSpawnEvent;
            _inputs = new PlayerInputs();
            UnpauseGame();
            PlayerController.OnPause += PauseGame;
            StartCoroutine(DeductDues());
            Visitor.OnPooStepping    += HandleGameOver;
        }
        else {Destroy(gameObject);}
    }

    void OnDestroy() {Debug.Log("Game Manager Destroyed"); if (Instance == this) { Instance = null; } }

    public int TaxDollars
    {
        set
        {
            _taxDollars = value;
            UpdateText();
        }
        get => _taxDollars;
    }

    private void HandleGameOver(Visitor obj)
    {
        if (_gameOver) { return;}
        _ohNo?.Invoke();
        StartCoroutine(GoBackToMainMenu());
        _gameOver = true;
    }

    public void HandleBonkableSpawnEvent(Bonkable bonkable, bool active)
    {
        if(active)
        {
            Bonkables.Add(bonkable);
            return;
        }

        Bonkables.Remove(bonkable);
    }
    

    public void RegisterVisitor() { TaxDollars = _taxDollars + 1; }
    
    public void UnRegisterVisitor()
    {
        if (!VistorPathManager.Instance) { return;}
        _visitorsLeft++;
        VistorPathManager.Instance.VisitorSpawnFrequency = 4 - Mathf.Log10(_visitorsLeft * 2);
        if (_visitorsLeft - _visitorsSinceLastPath > _visitorsTillNextPath)
        {
            _visitorsTillNextPath *= _difficultyCurve;
            VistorPathManager.Instance.AddPath();
            _visitorsSinceLastPath = _visitorsLeft;
        }
    }

    public void RegisterStaff(ParkStaff staff) { _staffCost += staff.StaffCost; }

    public void UnRegisterStaff(ParkStaff staff) { _staffCost -= staff.StaffCost; }
    
    public void PauseGame()
    {
        _inputs.UI.Enable();
        _settingsMenu.SetActive(true);
        Time.timeScale = 0;
    }

    public void UnpauseGame()
    {
        _inputs.Gameplay.Enable();
        _settingsMenu?.SetActive(false);
        Time.timeScale = 1;
    }

    void UpdateText()
    {
        _taxDollarText.text = $"${_taxDollars}";
    }

    IEnumerator DeductDues()
    {
        while (true)
        {
            yield return new WaitForSeconds(_deductionTime);
            TaxDollars = _taxDollars - _staffCost;
            if (_taxDollars <= 0)
            {
                _taxDollars = 0;
                Destroy(GameObject.FindObjectOfType<ParkStaff>());
            }
        }
    }

    IEnumerator GoBackToMainMenu()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("MainMenu");
    }
}
