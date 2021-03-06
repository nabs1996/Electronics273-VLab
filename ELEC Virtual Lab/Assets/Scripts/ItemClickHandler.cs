using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;
using SpiceSharp.Validation;
using System.Text.RegularExpressions;

public class ItemClickHandler : MonoBehaviour
{
    public static SpawnableItem spawnableItem;
    public static GameObject buttonClicked;

    [SerializeField] GameObject _breadboardUI = null;
    [SerializeField] GameObject _bananaPlugs = null;

    [SerializeField] Image _wireImage = null;
    [SerializeField] Image _resistorImage = null;
    [SerializeField] Image _capacitorImage = null;

    [SerializeField] GameObject _placedImages = null;

    [SerializeField] TMP_InputField _potentioMeterTextBox = null;

    public static bool isBBSlotFree = false;
    private GameObject _pointA = null;
    private GameObject _pointB = null;
    public static List<GameObject> breadboardSlots = new List<GameObject>();
    public static List<GameObject> allSlots = new List<GameObject>();

    int wireResistance = 0;

    private List<bool> bananaPlugActive = new List<bool>() { false, false, false, false, false };

    [SerializeField] GameObject _agilentMachine = null;
    [SerializeField] GameObject _flukeMachine = null;
    [SerializeField] GameObject _powerSupplyMachine = null;
    [SerializeField] GameObject bananaSlotConnectionPanel = null;

    VoltageSource voltageSource = new VoltageSource("a");
    CurrentSource currentSource = new CurrentSource("b");
    private bool changingConnection = false;

    private int clickedBananaSlot;

    private List<string> B0Connections = new List<string>();
    private List<string> B1Connections = new List<string>();
    private List<string> B2Connections = new List<string>();
    private List<string> B3Connections = new List<string>();
    private List<string> B4Connections = new List<string>();

    private int delayCalulationByFrames = 0;

    void Start()
    {
        Debug.Log("Adding slots");
        allSlots.Clear();
        int allSlotIDCounter = 0;

        for (int i = 0; i < _bananaPlugs.transform.childCount; i++)
        {
            var child = _bananaPlugs.transform.GetChild(i);
            if (child.CompareTag("BBSlot"))
            {
                allSlots.Add(child.gameObject);
                child.GetComponent<Slot>().slotID = allSlotIDCounter++;
            }
        }

        for (int i = 0; i < _breadboardUI.transform.childCount; i++)
        {
            var child = _breadboardUI.transform.GetChild(i);
            if (child.CompareTag("BBSlot"))
            {
                if (child.GetComponent<Slot>().slotType == Globals.SlotType.defaultSlot)
                {
                    //breadboardSlots.Add(child.gameObject);
                    allSlots.Add(child.gameObject);

                    child.GetComponent<Slot>().slotID = allSlotIDCounter++;
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

        if (!bananaSlotConnectionPanel.activeSelf)
        {
            if (Input.GetMouseButton(0))
            {
                ConnectBananaSlotsTogether();
            }
        }
        WaitForClick();
        if (Input.GetMouseButtonDown(1))
        {
            RemoveComponent();
        }

        if (changingConnection)
        {
            if (bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().OptionClicked)
            {
                //Globals.PSConnections[clickedInput] = bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked;
                //Debug.Log("Updating Key: PSConnectios[" + clickedInput + "] To: " + bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked);
                AddBananaSlotConnectionToBananaSlot();
                bananaSlotConnectionPanel.SetActive(false);
                bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().OptionClicked = false;
                changingConnection = false;
                clickedBananaSlot = -1;
            }
        }
        // function attached to a button for testing

        if(delayCalulationByFrames > 80)
        {   
            SpiceSharpCalculation();
            delayCalulationByFrames = 0;
        }
        delayCalulationByFrames++;

    }



    public void SpiceSharpCalculation()
    {
        Debug.Log("*********************");
        Debug.Log("Starting calculation ");
        ClearAllSlots();

        voltageSource = new VoltageSource("a");
        currentSource = new CurrentSource("b");
        var ckt = new Circuit();


        foreach (KeyValuePair<Globals.AgilentInput, Globals.BananaPlugs> entry in Globals.AgilentConnections)
        {
            if (entry.Value != Globals.BananaPlugs.noConnection)
            {
                ActivateBananaSlot(entry.Value.ToString());
            }
        }

        foreach (KeyValuePair<Globals.FlukeInput, Globals.BananaPlugs> entry in Globals.FlukeConnections)
        {
            if (entry.Value != Globals.BananaPlugs.noConnection)
            {
                ActivateBananaSlot(entry.Value.ToString());
            }
        }

        foreach (KeyValuePair<Globals.PowerSupplyInput, Globals.BananaPlugs> entry in Globals.PSConnections)
        {
            if (entry.Value != Globals.BananaPlugs.noConnection)
            {
                AddPowerSupplyConnection(entry.Key.ToString(), entry.Value.ToString(), ckt);
                ActivateBananaSlot(entry.Value.ToString());
            }
        }

        //Handle Self BananaPlug Connections
        for (int i = 0; i < 5; i++)
        {
            if (bananaPlugActive[i])
            {
                HandleSelfBananaConnections(i, ckt);
            }
        }

        //INSERT POTENTIOMETER VAL HERE


        if (bananaPlugActive[4])
        {
            if(_potentioMeterTextBox.text.Length == 0)
            {
                ckt.Add(new Resistor("PotentioMeterBanana", "B4", "PotentioMeter", 0));
                Debug.Log("Adding Wire: PotentioMeterBanana B4 PotentioMeter 0");
            }
            else
            {
                ckt.Add(new Resistor("PotentioMeterBanana", "B4", "PotentioMeter", int.Parse(_potentioMeterTextBox.text)));
                Debug.Log("Adding Wire: PotentioMeterBanana B4 PotentioMeter " + _potentioMeterTextBox.text);
            }
            
            ckt.Add(new Resistor("PotentioMeterBlack", "PotentioMeter", "C67", 0));
            ckt.Add(new Resistor("PotentioMeterBlue", "PotentioMeter", "C70", 0));
            ckt.Add(new Resistor("PotentioMeterRed", "PotentioMeter", "C72", 0));
            Debug.Log("Adding Wire: PotentioMeterBlack PotentioMeter C67 0");
            Debug.Log("Adding Wire: PotentioMeterBlue PotentioMeter C70 0");
            Debug.Log("Adding Wire: PotentioMeterRed PotentioMeter C82 0");
        }
        if (bananaPlugActive[2] || bananaPlugActive[4])
        {
            ckt.Add(new Inductor("B2B4Inductor", "B2", "B4", 0.020f));
            Debug.Log("Adding Inductor: PotentioMeterInductor B2 B4 0.020H");
        }
        // ckt.Add(new Resistor("PotentioMeterBanana", "B4", "PotentioMeter", 500));
        // ckt.Add(new Resistor("PotentioMeterBlack", "PotentioMeter", "C17", 0));
        // ckt.Add(new Resistor("PotentioMeterBlue", "PotentioMeter", "C18", 0));
        // ckt.Add(new Resistor("PotentioMeterRed", "PotentioMeter", "C19", 0));
        // ckt.Add(new Resistor("ShortCircuitInductor", "B2", "B4", 0));
        // ckt.Add(new Inductor("ShortCircuitInductor","B2", "B4",0.020f));

        //handle permanent connections
        for (int i = 0; i < _bananaPlugs.transform.childCount; i++)
        {
            
            if (bananaPlugActive[i])
            {
                var child = _bananaPlugs.transform.GetChild(i);
                if (child.CompareTag("BBSlot"))
                {
                    AddElectricalElement(child.GetComponent<Slot>(), ckt);
                    child.GetComponent<Slot>().slotChecked = true;
                    child.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotChecked = true;
                }
            }
        }
        for (int i = 0; i < _breadboardUI.transform.childCount; i++)
        {
            var child = _breadboardUI.transform.GetChild(i);
            if (child.CompareTag("BBSlot"))
            {
                if (child.GetComponent<Slot>().itemPlaced != null && !child.GetComponent<Slot>().slotChecked && !child.GetComponent<Slot>().ignoreThisSlot)
                {
                    if (child.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotType == Globals.SlotType.defaultSlot)
                    {
                        AddElectricalElement(child.GetComponent<Slot>(), ckt);
                        child.GetComponent<Slot>().slotChecked = true;
                        child.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotChecked = true;
                    }
                }
            }
            // if (breadboardSlots[i].GetComponent<Slot>().itemPlaced != null && !breadboardSlots[i].GetComponent<Slot>().slotChecked)
            // {
            //     if (breadboardSlots[i].GetComponent<Slot>().slotPair.GetComponent<Slot>().slotType == Globals.SlotType.defaultSlot)
            //     {

            //         AddElectricalElement(breadboardSlots[i].GetComponent<Slot>(), ckt);
            //         breadboardSlots[i].GetComponent<Slot>().slotChecked = true;
            //         breadboardSlots[i].GetComponent<Slot>().slotPair.GetComponent<Slot>().slotChecked = true;
            //     }
            // }
        }

        // VoltageSource powerSupply = AddVoltageSource(ckt);
        // ckt.Add(powerSupply);


        // Create a DC sweep and register to the event for exporting simulation data
        //var dc = new DC("dc", "voltageSourcePS", _powerSupply.GetComponent<PowerSupply>().powerReading, _powerSupply.GetComponent<PowerSupply>().powerReading, 1);
        //var dc = new DC("dc", "voltageSourcePS", _powerSupplyMachine.GetComponent<PSSelect>().voltage, _powerSupplyMachine.GetComponent<PSSelect>().voltage, 1);

        var dc = new DC("dc");
        if (voltageSource.Name != "a" && currentSource.Name != "b")
        {
            dc = new DC("dc", new[]{
            new ParameterSweep(voltageSource.Name,new LinearSweep(_powerSupplyMachine.GetComponent<PSSelect>().voltage,_powerSupplyMachine.GetComponent<PSSelect>().voltage,1)),
            new ParameterSweep(currentSource.Name,new LinearSweep(_powerSupplyMachine.GetComponent<PSSelect>().current,_powerSupplyMachine.GetComponent<PSSelect>().current,1))
        });
        }
        else if (voltageSource.Name != "a")
        {
            dc = new DC("dc", new[]{
            new ParameterSweep(voltageSource.Name,new LinearSweep(_powerSupplyMachine.GetComponent<PSSelect>().voltage,_powerSupplyMachine.GetComponent<PSSelect>().voltage,1))
        });
        }
        else if (currentSource.Name != "b")
        {
            dc = new DC("dc", new[]{
             new ParameterSweep(currentSource.Name,new LinearSweep(_powerSupplyMachine.GetComponent<PSSelect>().current,_powerSupplyMachine.GetComponent<PSSelect>().current,1))
        });
        }


        //GetCircuitToolsReading(dc);
        ManageAgilentReadingsConnections(dc);
        ManageFlukeReadingsConnections(dc);
        // Run the simulation
        try
        {
            dc.Run(ckt);
        }
        catch (ValidationFailedException e)
        {
            Debug.Log("Error is: " + e.ToString());
        }

    }



    private void ClearAllSlots()
    {
        for (int i = 0; i < bananaPlugActive.Count; i++)
        {
            bananaPlugActive[i] = false;
        }

        for (int i = 0; i < _breadboardUI.transform.childCount; i++)
        {
            var child = _breadboardUI.transform.GetChild(i);

            if (child.CompareTag("BBSlot"))
            {
                child.GetComponent<Slot>().slotChecked = false;
            }
        }

        for (int i = 0; i < _bananaPlugs.transform.childCount; i++)
        {
            var child = _bananaPlugs.transform.GetChild(i);
            if (child.CompareTag("BBSlot"))
            {
                child.GetComponent<Slot>().slotChecked = false;
            }
        }
    }

    // private VoltageSource AddVoltageSource(Circuit ckt)
    // {
    //     VoltageSource source = new VoltageSource("PS", "PSPOS", "0", 0);


    //     for (int i = 0; i < _powerSupply.transform.childCount; i++)
    //     {
    //         var child = _powerSupply.transform.GetChild(i);
    //         if (child.CompareTag("BBSlot"))
    //         {
    //             if (child.GetComponent<Slot>().itemPlaced != null && !child.GetComponent<Slot>().slotChecked)
    //             {
    //                 AttachPowerSupply(child, ckt);
    //             }
    //         }
    //     }
    //     return source;
    // }

    private void AddPowerSupplyConnection(string pos, string neg, Circuit ckt)
    {
        bool isGroundConnected = false;
        if (Globals.PSConnections.TryGetValue(Globals.PowerSupplyInput.ground, out Globals.BananaPlugs groundConnection))
        {
            if (!groundConnection.Equals(Globals.BananaPlugs.noConnection))
            {
                isGroundConnected = true;
            }
        }

        if (pos == Globals.PowerSupplyInput.ground.ToString())
        {
            pos = "0";
        }
        else if (pos == Globals.PowerSupplyInput.voltageSource.ToString() && isGroundConnected)
        {
            Debug.Log("Adding PS Voltage Source: " + pos + "PS" + " " + pos + " " + "0" + " " + _powerSupplyMachine.GetComponent<PSSelect>().voltage);
            VoltageSource voltSource = new VoltageSource(pos + "PS", pos, "0", _powerSupplyMachine.GetComponent<PSSelect>().voltage);
            voltageSource = voltSource;
            ckt.Add(voltSource);
        }
        else if (pos == Globals.PowerSupplyInput.currentSource.ToString() && isGroundConnected)
        {
            Debug.Log("Adding PS Current Source: " + pos + "PS" + " " + pos + " " + "0" + " " + _powerSupplyMachine.GetComponent<PSSelect>().current);
            CurrentSource currSource = new CurrentSource(pos + "PS", pos, "0", _powerSupplyMachine.GetComponent<PSSelect>().current);
            currentSource = currSource;
            ckt.Add(currSource);
        }

        Debug.Log("Adding Wire: " + pos + " " + pos + " " + neg + " " + 0);
        ckt.Add(new Resistor(pos, pos, neg, 0));
    }

    // private void AttachPowerSupply(Transform slot, Circuit ckt)
    // {
    //     if (slot.name.Equals("PS Pos Slot"))
    //     {
    //         if (slot.GetComponent<Slot>().itemPlaced != null && !slot.GetComponent<Slot>().slotChecked)
    //         {
    //             AddElectricalElement(slot.GetComponent<Slot>(), ckt, "PSPOS",
    //                 "C" + GetSlotColumn(slot.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotID).ToString());
    //             slot.GetComponent<Slot>().slotChecked = true;
    //             slot.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotChecked = true;
    //         }
    //     }
    //     if (slot.name.Equals("PS Neg Slot"))
    //     {
    //         if (slot.GetComponent<Slot>().itemPlaced != null && !slot.GetComponent<Slot>().slotChecked)
    //         {
    //             AddElectricalElement(slot.GetComponent<Slot>(), ckt, "C" + GetSlotColumn(slot.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotID).ToString(),
    //                 "0");
    //             slot.GetComponent<Slot>().slotChecked = true;
    //             slot.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotChecked = true;
    //         }
    //     }
    // }

    private void ManageAgilentReadingsConnections(DC dc)
    {
        //Voltmeter
        string volPos = "+";
        string AgileGroundSlot = "-";
        string currentSlot = "i";
        if (Globals.AgilentConnections.TryGetValue(Globals.AgilentInput.voltageInput, out Globals.BananaPlugs posVolNode))
        {
            if (posVolNode != Globals.BananaPlugs.noConnection)
            {
                volPos = GetBananaConnections(posVolNode.ToString());
            }

        }
        if (Globals.AgilentConnections.TryGetValue(Globals.AgilentInput.groundInput, out Globals.BananaPlugs groundNode))
        {

            if (groundNode != Globals.BananaPlugs.noConnection)
            {
                AgileGroundSlot = GetBananaConnections(groundNode.ToString());
            }

        }
        if (Globals.AgilentConnections.TryGetValue(Globals.AgilentInput.currentInput, out Globals.BananaPlugs currentNode))
        {
            if (currentNode != Globals.BananaPlugs.noConnection)
            {
                currentSlot = GetBananaConnections(currentNode.ToString());
            }

        }

        dc.ExportSimulationData += (sender, exportDataEventArgs) =>
        {
            Debug.Log("Agilent reading from: " + volPos + " and: " + AgileGroundSlot);
            Debug.Log("AGILENT VOLTAGE READING: " + new RealVoltageExport(dc, volPos, AgileGroundSlot).Value.ToString());
            _agilentMachine.GetComponent<AgilentSelect>().voltageReading = (float)new RealVoltageExport(dc, volPos, AgileGroundSlot).Value;
            _agilentMachine.GetComponent<AgilentSelect>().valueUpdated = true;
        };

        dc.ExportSimulationData += (sender, exportDataEventArgs) =>
        {
            // Debug.Log("AGILENT CURRENT READING: " + (new RealCurrentExport(dc, currentSource.Name)).Value);
            Debug.Log("AGILENT CURRENT READING: " + (new RealCurrentExport(dc, voltageSource.Name)).Value.ToString());
            _agilentMachine.GetComponent<AgilentSelect>().currentReading = (float)new RealCurrentExport(dc, voltageSource.Name).Value;
            _agilentMachine.GetComponent<AgilentSelect>().valueUpdated = true;
        };

    }

    private void ManageFlukeReadingsConnections(DC dc)
    {
        //Voltmeter
        string volPos = "+";
        string flukeGroundSlot = "-";
        string currentSlot = "i";
        if (Globals.FlukeConnections.TryGetValue(Globals.FlukeInput.voltageInput, out Globals.BananaPlugs posVolNode))
        {
            if (posVolNode != Globals.BananaPlugs.noConnection)
            {
                volPos = GetBananaConnections(posVolNode.ToString());
            }

        }
        if (Globals.FlukeConnections.TryGetValue(Globals.FlukeInput.groundInput, out Globals.BananaPlugs groundNode))
        {

            if (groundNode != Globals.BananaPlugs.noConnection)
            {
                flukeGroundSlot = GetBananaConnections(groundNode.ToString());
            }

        }
        if (Globals.FlukeConnections.TryGetValue(Globals.FlukeInput.currentInput, out Globals.BananaPlugs currentNode))
        {
            if (currentNode != Globals.BananaPlugs.noConnection)
            {
                currentSlot = GetBananaConnections(currentNode.ToString());
            }

        }

        dc.ExportSimulationData += (sender, exportDataEventArgs) =>
        {
            Debug.Log("Fluke reading from: " + volPos + " and: " + flukeGroundSlot);
            Debug.Log("Fluke VOLTAGE READING: " + new RealVoltageExport(dc, volPos, flukeGroundSlot).Value.ToString());
            _flukeMachine.GetComponent<FlukeSelect>().voltageReading = (float)new RealVoltageExport(dc, volPos, flukeGroundSlot).Value;
            _flukeMachine.GetComponent<FlukeSelect>().valueUpdated = true;
        };

        dc.ExportSimulationData += (sender, exportDataEventArgs) =>
        {
            // Debug.Log("Fluke CURRENT READING: " + (new RealCurrentExport(dc, currentSource.Name)).Value);
            //Debug.Log("Fluke CURRENT READING: " + (new RealCurrentExport(dc, voltageSource.Name)).Value.ToString());
            Debug.Log("Current source: " + currentSource.Name + " " + currentSource.Parameters.DcValue.ToString());
            Debug.Log("Fluke Current reading: " + (new RealCurrentExport(dc, voltageSource.Name)).Value.ToString());
            _flukeMachine.GetComponent<FlukeSelect>().currentReading = (float)new RealCurrentExport(dc, voltageSource.Name).Value;
            _flukeMachine.GetComponent<FlukeSelect>().valueUpdated = true;
        };

    }

    private string GetBananaConnections(String BananaPlugSlot)
    {
        int removedInt = int.Parse(Regex.Match(BananaPlugSlot, @"\d+").Value);
        string columnConnection = "";
        
        switch (removedInt)
        {
            case 0:
                columnConnection = "C20";
                break;

            case 1:
                columnConnection = "C0";
                break;

            case 2:
                columnConnection = "C5";
                break;

            case 3:
                columnConnection = "C10";
                break;

            case 4:
                columnConnection = "C15";
                break;

            default:
                Debug.Log("No banana Plug found");
                break;
        }

        return columnConnection;
    }

    private void ActivateBananaSlot(String value)
    {

        int removedInt = int.Parse(Regex.Match(value, @"\d+").Value);
        bananaPlugActive[removedInt] = true;
    }

    private void AddElectricalElement(Slot slot, Circuit ckt)
    {

        string componentName = "S" + slot.slotID;
        string componentStart = "C" + GetSlotColumn(slot.slotID);
        string componentEnd = "C" + GetSlotColumn(slot.slotPair.GetComponent<Slot>().slotID);

        if (slot.slotType == Globals.SlotType.BananaPlugSlot)
        {
            componentStart = "B" + slot.slotID;
        }

        float value = GetComponentValue(slot);
        switch (slot.itemPlaced.itemName)
        {
            case (Globals.AvailableItems.Wire):
                Resistor wire = new Resistor(componentName, componentStart, componentEnd, wireResistance);
                Debug.Log("Adding Wire: " + componentName + " " + componentStart + " " + componentEnd + " " + wireResistance);
                ckt.Add(wire);
                break;

            case (Globals.AvailableItems.Resistor):
                Resistor resistor = new Resistor(componentName, componentStart, componentEnd, value);
                Debug.Log("Adding Resistor: " + componentName + " " + componentStart + " " + componentEnd + " " + value);
                ckt.Add(resistor);
                break;

            case (Globals.AvailableItems.Capacitor):
                Capacitor capacitor = new Capacitor(componentName, componentStart, componentEnd, value);
                Debug.Log("Adding Capacitor: " + componentName + " " + componentStart + " " + componentEnd + " " + value);
                ckt.Add(capacitor);
                break;

            default:
                Debug.Log("Nothing found");
                break;
        }
    }

    // private void AddElectricalElement(Slot slot, Circuit ckt, string componentStart, string componentEnd)
    // {
    //     string componentName = "S" + slot.slotID;
    //     float value = GetComponentValue(slot);

    //     switch (slot.itemPlaced.itemName)
    //     {
    //         case (Globals.AvailableItems.Wire):
    //             Resistor wire = new Resistor(componentName, componentStart, componentEnd, wireResistance);
    //             Debug.Log("Adding Wire: " + componentName + " " + componentStart + " " + componentEnd + " " + wireResistance);
    //             ckt.Add(wire);
    //             break;

    //         case (Globals.AvailableItems.Resistor):
    //             Resistor resistor = new Resistor(componentName, componentStart, componentEnd, value);
    //             Debug.Log("Adding Resistor: " + componentName + " " + componentStart + " " + componentEnd + " " + value);
    //             ckt.Add(resistor);
    //             break;

    //         case (Globals.AvailableItems.Capacitor):
    //             Capacitor capacitor = new Capacitor(componentName, componentStart, componentEnd, value);
    //             Debug.Log("Adding Capacitor: " + componentName + " " + componentStart + " " + componentEnd + " " + value);
    //             ckt.Add(capacitor);
    //             break;

    //         default:
    //             break;
    //     }

    // }

    private int GetSlotColumn(int slot)
    {
        int column = -1;
        slot -= 5;
        column = (int)Math.Floor((double)(slot / 5.0));
        //VCC Slots are all considered the same
        if (column < 5)
        {
            column = 0;
        }
        else if (column < 10)
        {
            column = 5;
        }
        else if (column < 15)
        {
            column = 10;
        }
        else if (column < 20)
        {
            column = 15;
        }
        return column;
    }

    public static void ItemClicked()
    {
        if (Globals.mouseClickAction == Globals.MouseClickAction.NoClick && spawnableItem != null)
        {
            // Debug.Log("item clicked");
            ItemClickToHandle();
        }
    }

    private static void ItemClickToHandle()
    {
        switch (spawnableItem.itemName)
        {
            case Globals.AvailableItems.Wire:
                WireClicked();
                break;
            case Globals.AvailableItems.Resistor:
                ResistorClicked();
                break;
            case Globals.AvailableItems.Capacitor:
                CapacitorClicked();
                break;
            default:
                Debug.Log("Click not recognized");
                break;
        }
    }

    // private void GetCircuitToolsReading(DC dc)
    // {
    //     string posToolSlot = "+";
    //     string negToolSlot = "-";
    //     var meterMode = _toolsMeter.GetComponentInChildren<TMP_Dropdown>().value;
    //     Transform meterVal = null;

    //     for (int i = 0; i < _toolsMeter.transform.childCount; i++)
    //     {
    //         var child = _toolsMeter.transform.GetChild(i);
    //         if (child.CompareTag("BBSlot"))
    //         {
    //             if (child.GetComponent<Slot>().itemPlaced != null && !child.GetComponent<Slot>().slotChecked)
    //             {
    //                 if (child.name.Equals("Pos Slot"))
    //                 {
    //                     posToolSlot = "C" + GetSlotColumn(child.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotID).ToString();
    //                 }
    //                 else if (child.name.Equals("Neg Slot"))
    //                 {
    //                     negToolSlot = "C" + GetSlotColumn(child.GetComponent<Slot>().slotPair.GetComponent<Slot>().slotID).ToString();
    //                 }
    //             }
    //         }
    //         else if (child.name.Equals("Meter Val"))
    //         {
    //             meterVal = child;
    //         }
    //     }
    //     Debug.Log("Pos slot: " + posToolSlot);
    //     Debug.Log("Neg slot: " + negToolSlot);
    //     switch (meterMode)
    //     {
    //         case 0: //VoltMeter
    //             dc.ExportSimulationData += (sender, exportDataEventArgs) =>
    //             {

    //                 Debug.Log("GETTING READING BETWEEN " + posToolSlot + " AND " + negToolSlot);
    //                 Debug.Log("Real voltage " + (new RealVoltageExport(dc, posToolSlot, negToolSlot)).Value);
    //                 meterVal.GetComponent<TMP_Text>().text = new RealVoltageExport(dc, posToolSlot, negToolSlot).Value.ToString("0.0000") + " V";
    //             };
    //             break;

    //         case 1: //Ammeter
    //             dc.ExportSimulationData += (sender, exportDataEventArgs) =>
    //             {
    //                 Debug.Log("Real Current " + (new RealCurrentExport(dc, "PS")).Value);
    //                 meterVal.GetComponent<TMP_Text>().text = new RealCurrentExport(dc, "PS").Value.ToString() + " A";
    //             };
    //             break;
    //         default:
    //             break;
    //     }
    // }

    private static void WireClicked()
    {
        // Debug.Log("I clicked: " + spawnableItem.itemValue + " " + spawnableItem.itemName + " with ID: " + spawnableItem.itemID);
        Globals.mouseClickAction = Globals.MouseClickAction.TwoClicks_FirstClick;
    }

    private static void ResistorClicked()
    {
        // Debug.Log("I clicked: " + spawnableItem.itemValue + " " + spawnableItem.itemName + " with ID: " + spawnableItem.itemID);
        Globals.mouseClickAction = Globals.MouseClickAction.TwoClicks_FirstClick;
    }

    private static void CapacitorClicked()
    {
        // Debug.Log("I clicked: " + spawnableItem.itemValue + " " + spawnableItem.itemName + " with ID: " + spawnableItem.itemID);
        Globals.mouseClickAction = Globals.MouseClickAction.TwoClicks_FirstClick;
    }

    private void WaitForClick()
    {   

        switch (Globals.mouseClickAction)
        {
            case Globals.MouseClickAction.TwoClicks_FirstClick:
                if (Input.GetMouseButton(0))
                {
                    CheckIfBBSlot();
                    if (isBBSlotFree)
                    {
                        // Debug.Log("First Click GOOD");
                        _pointA.GetComponent<Slot>().PlaceItem();
                        isBBSlotFree = false;
                    }
                }
                break;

            case Globals.MouseClickAction.TwoClicks_SecondClick:
            
                if (Input.GetMouseButton(0))
                {
                    CheckIfBBSlot();
                    if (isBBSlotFree)
                    {
                        // Debug.Log("Second Click GOOD DRAWING LINE");
                        _pointB.GetComponent<Slot>().PlaceItem();
                        _pointB.GetComponent<Slot>().itemPlaced = spawnableItem;
                        _pointA.GetComponent<Slot>().itemPlaced = spawnableItem;
                        
                        _pointB.GetComponent<Slot>().slotPair = _pointA;
                        _pointA.GetComponent<Slot>().slotPair = _pointB;
                        DrawLineBetweenPoints();
                        RemoveItemButtonInList();

                        isBBSlotFree = false;
                    }
                }
                break;

            default:
                break;
        }
    }

    private void RemoveItemButtonInList()
    {
        spawnableItem.isPlaced = true;
        Destroy(buttonClicked);
    }

    private void DrawLineBetweenPoints()
    {

        Image placeItem = null;


        switch (spawnableItem.itemName)
        {
            case (Globals.AvailableItems.Wire):
                placeItem = Instantiate(_wireImage, _placedImages.transform);
                placeItem.GetComponent<Image>().color = Resources.Load<Image>(
                spawnableItem.itemName.ToString() +
                spawnableItem.itemValue.ToString()).color;
                break;

            case (Globals.AvailableItems.Resistor):
                placeItem = Instantiate(_resistorImage, _placedImages.transform);
                break;

            case (Globals.AvailableItems.Capacitor):
                placeItem = Instantiate(_capacitorImage, _placedImages.transform);
                break;

            default:
                placeItem = Instantiate(_wireImage, _placedImages.transform);
                break;
        }

        placeItem.gameObject.AddComponent<PlacedImages>();
        placeItem.GetComponent<PlacedImages>().itemID = spawnableItem.itemID;
        placeItem.GetComponent<PlacedImages>().slotA = _pointA.GetComponent<Slot>().slotID;
        placeItem.GetComponent<PlacedImages>().slotB = _pointB.GetComponent<Slot>().slotID;
        placeItem.tag = "PlacedItem";


        float distance = Vector2.Distance(_pointA.transform.position, _pointB.transform.position);
        // Debug.Log("Distance: " + distance);
        distance = distance * 0.62f; //Have it be slightly shorter so it gets the centers

        float rotation = AngleBetweenVector2(_pointA.transform.position, _pointB.transform.position);
        float posX = PositionXForLine(_pointA.transform.position, _pointB.transform.position);
        float posY = PositionYForLine(_pointA.transform.position, _pointB.transform.position);

        placeItem.transform.position = new Vector2(posX, posY);
        placeItem.rectTransform.sizeDelta = new Vector2(distance, placeItem.rectTransform.sizeDelta.y);
        placeItem.transform.Rotate(0, 0, rotation, Space.Self);

        //reset the points
        _pointA = null;
        _pointB = null;
    }

    private void ResetWire()
    {
        _wireImage.transform.position = new Vector3(0, 0, 0);
        _wireImage.transform.Rotate(0, 0, 0, Space.World);
        _wireImage.rectTransform.sizeDelta = new Vector2(25, _wireImage.rectTransform.sizeDelta.y);
    }

    private float AngleBetweenVector2(Vector2 vec1, Vector2 vec2)
    {
        Vector2 difference = vec2 - vec1;
        float sign = (vec2.y < vec1.y) ? -1.0f : 1.0f;
        return Vector2.Angle(Vector2.right, difference) * sign;
    }

    //Gets the center X between both points
    private float PositionXForLine(Vector2 vec1, Vector2 vec2)
    {
        float x1 = vec1.x;
        float x2 = vec2.x;
        return ((x1 + x2) / 2.0f);
    }

    //Gets the center Y between both points
    private float PositionYForLine(Vector2 vec1, Vector2 vec2)
    {
        float y1 = vec1.y;
        float y2 = vec2.y;
        return ((y1 + y2) / 2.0f);
    }

    private void CheckIfBBSlot()
    {
        isBBSlotFree = false;
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);
        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.transform.CompareTag("BBSlot"))
                {   
                    isBBSlotFree = results[i].gameObject.transform.GetComponent<Slot>().isFree;

                    if (Globals.mouseClickAction == Globals.MouseClickAction.TwoClicks_FirstClick)
                    {
                        _pointA = results[i].gameObject;
                    }
                    else if (Globals.mouseClickAction == Globals.MouseClickAction.TwoClicks_SecondClick)
                    {
                        _pointB = results[i].gameObject;
                    }
                }
            }
        }
    }

    private void RemoveComponent()
    {
        GameObject itemToRemove;
        itemToRemove = CheckIfPlacedComponent();

        if (itemToRemove != null)
        {
            Debug.Log("Removing item between slots: " + itemToRemove.GetComponent<PlacedImages>().slotA + " and " + itemToRemove.GetComponent<PlacedImages>().slotB);
            allSlots[itemToRemove.GetComponent<PlacedImages>().slotA].GetComponent<Slot>().RemoveItem();
            allSlots[itemToRemove.GetComponent<PlacedImages>().slotB].GetComponent<Slot>().RemoveItem();
            Globals.inventoryItems[itemToRemove.GetComponent<PlacedImages>().itemID].isPlaced = false;
        }
        Destroy(itemToRemove);
    }

    private GameObject CheckIfPlacedComponent()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);
        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.transform.CompareTag("PlacedItem"))
                {
                    return results[i].gameObject;

                }
            }
        }
        return null;
    }

    private void ConnectBananaSlotsTogether()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);
        if (results.Count > 0)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject.transform.CompareTag("clickable"))
                {
                    bananaSlotConnectionPanel.SetActive(true);
                    changingConnection = true;
                    clickedBananaSlot = results[i].gameObject.GetComponentInParent<Slot>().slotID;
                }
            }
        }
    }

    private void AddBananaSlotConnectionToBananaSlot()
    {
        Debug.Log("Adding connections for: B"+ clickedBananaSlot);
        switch (clickedBananaSlot)
        {
            case (0):
                if (bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.Equals(Globals.BananaPlugs.noConnection))
                {
                    B0Connections.Clear();
                }
                else
                {
                    Debug.Log("Adding :" + bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                    B0Connections.Add(bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                }
                break;

            case (1):
                if (bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.Equals(Globals.BananaPlugs.noConnection))
                {
                    B1Connections.Clear();
                }
                else
                {
                    Debug.Log("Adding :" + bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                    B1Connections.Add(bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                }
                break;


            case (2):
                if (bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.Equals(Globals.BananaPlugs.noConnection))
                {
                    B2Connections.Clear();
                }
                else
                {
                    Debug.Log("Adding :" + bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                    B2Connections.Add(bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                }
                break;


            case (3):
                if (bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.Equals(Globals.BananaPlugs.noConnection))
                {
                    B3Connections.Clear();
                }
                else
                {
                    Debug.Log("Adding :" + bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                    B3Connections.Add(bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                }
                break;


            case (4):
                if (bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.Equals(Globals.BananaPlugs.noConnection))
                {
                    B4Connections.Clear();
                }
                else
                {
                    Debug.Log("Adding :" + bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                    B4Connections.Add(bananaSlotConnectionPanel.GetComponent<BreadboardBananaConnectionPanelButtons>().BananaPlugsSlotClicked.ToString());
                }
                break;

            default:
                Debug.Log("idk");
                break;
        }

    }

    private void HandleSelfBananaConnections(int i, Circuit ckt)
    {
        switch (i)
        {
            case (0):
                foreach (String connection in B0Connections.Distinct().ToList())
                {
                    Debug.Log("Adding wire:" + "B" + i + connection + " B" + i + " " + connection);
                    ckt.Add(new Resistor("B" + i + connection, "B" + i, connection, 0));
                    ActivateBananaSlot(connection);
                }
                break;

            case (1):
                foreach (String connection in B1Connections.Distinct().ToList())
                {
                    Debug.Log("Adding wire:" + "B" + i + connection + " B" + i + " " + connection);
                    ckt.Add(new Resistor("B" + i + connection, "B" + i, connection, 0));
                    ActivateBananaSlot(connection);
                }
                break;


            case (2):
                foreach (String connection in B2Connections.Distinct().ToList())
                {
                    Debug.Log("Adding wire:" + "B" + i + connection + " B" + i + " " + connection);
                    ckt.Add(new Resistor("B" + i + connection, "B" + i, connection, 0));
                    ActivateBananaSlot(connection);
                }
                break;


            case (3):
                foreach (String connection in B3Connections.Distinct().ToList())
                {
                    Debug.Log("Adding wire:" + "B" + i + connection + " B" + i + " " + connection);
                    ckt.Add(new Resistor("B" + i + connection, "B" + i, connection, 0));
                    ActivateBananaSlot(connection);
                }
                break;

            case (4):
                foreach (String connection in B4Connections.Distinct().ToList())
                {
                    Debug.Log("Adding wire:" + "B" + i + connection + " B" + i + " " + connection);
                    ckt.Add(new Resistor("B" + i + connection, "B" + i, connection, 0));
                    ActivateBananaSlot(connection);
                }
                break;

            default:
                Debug.Log("idk");
                break;
        }
    }

    private float GetComponentValue(Slot slot)
    {
        float componentVal = 0;
        if (slot.itemPlaced.itemName == Globals.AvailableItems.Resistor)
        {
            switch (slot.itemPlaced.itemValue)
            {
                case (0):
                    componentVal = 210;
                    break;

                case (1):
                    componentVal = 370;
                    break;

                case (2):
                    componentVal = 480;
                    break;

                default:
                    break;
            }
        }
        else if (slot.itemPlaced.itemName == Globals.AvailableItems.Capacitor)
        {
            switch (slot.itemPlaced.itemValue)
            {
                case (0):
                    componentVal = 0.0000001f;
                    break;

                case (1):
                    componentVal = 0.000001f;
                    break;

                case (2):
                    componentVal = 0.000004f;
                    break;
                case (3):
                    componentVal = 0.000005f;
                    break;

                case (4):
                    componentVal = 0.000009f;
                    break;

                case (5):
                    componentVal = 0.000012f;
                    break;
                case (6):
                    componentVal = 0.000015f;
                    break;

                case (7):
                    componentVal = 0.000019f;
                    break;

                case (8):
                    componentVal = 0.000020f;
                    break;
                default:
                    break;
            }
            // componentVal = 1 / ((2 * (float)Math.PI * 60) * componentVal);
        }

        // float randomMultiplier = UnityEngine.Random.Range(0.95f,1.05f);
        // return componentVal * randomMultiplier;

        return componentVal;
    }
}