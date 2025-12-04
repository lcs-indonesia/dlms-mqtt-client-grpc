//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux products: http://www.gurux.org
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using System.Runtime.InteropServices;
using DLMS.Client.GXMedia.Mqtt;
using DlmsMqttClientGrpc.Application.DTOs.Dlms;
using DlmsMqttClientGrpc.Application.Exceptions;
using DlmsMqttClientGrpc.Application.Interfaces;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.Net;
using Gurux.Serial;

namespace DLMS.Client;

public class DLMSClient : IDisposable, IDlmsClient
{
    private bool isAssociationViewReaded = false;
    private bool isInitialized = false;
    private GXDLMSReader reader;
    private Settings settings;
    public DLMSClient(string[] args, Settings settings)
    {
        this.settings = settings;
        ////////////////////////////////////////
        //Handle command line parameters.
        int ret = Settings.GetParameters(args, settings);
        if (ret != 0) throw new ArgumentNullException("Argument can't be null.");
        settings.client.OnPdu += (sender, data) =>
        {
            try
            {
                /*
                //Encrypted PDUs are converted to XML.
                GXDLMSTranslator translator = new GXDLMSTranslator();
                translator.Comments = true;
                translator.SecuritySuite = settings.client.Ciphering.SecuritySuite;
                translator.BlockCipherKey = settings.client.Ciphering.BlockCipherKey;
                translator.AuthenticationKey = settings.client.Ciphering.AuthenticationKey;
                string xml = translator.PduToXml(data);
                Console.WriteLine(xml);
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        };
        ////////////////////////////////////////
        //Initialize connection settings.
        if (settings.media is GXSerial) { }
        else if (settings.media is GXNet) { }
        else if (settings.media is GXMqtt mqtt) { }
        else throw new Exception("Unknown media type.");
        ////////////////////////////////////////
        reader = new GXDLMSReader(settings.client,
            settings.media, settings.trace,
            settings.invocationCounter, settings.WaitTime);
        reader.OnNotification += (data) =>
        {
            Console.WriteLine(data);
        };
        //Create manufacturer spesific custom COSEM object.
        settings.client.OnCustomObject += (type, version) =>
        {
            /*
            if (type == 6001 && version == 0)
            {
                return new ManufacturerSpesificObject();
            }
            */
            return null;
        };

        try { settings.media.Open(); }
        catch (System.IO.IOException ex)
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine(ex.Message);
            Console.WriteLine("Available ports:");
            Console.WriteLine(string.Join(" ", GXSerial.GetPortNames()));
            return;
        }
        //Some meters need a break here.
        Thread.Sleep(1000);
        Console.WriteLine("Connected:");

        if (settings.media is GXNet net && settings.client.InterfaceType == InterfaceType.CoAP)
        {
            //Update token ID.  
            settings.client.Coap.Token = 0x45;
            settings.client.Coap.Host = net.HostName;
            settings.client.Coap.MessageId = 1;
            settings.client.Coap.Port = (UInt16)net.Port;
            //DLMS version.
            settings.client.Coap.Options[65001] = (byte)1;
            //Client SAP.
            settings.client.Coap.Options[65003] = (byte)settings.client.ClientAddress;
            //Server SAP
            settings.client.Coap.Options[65005] = (byte)settings.client.ServerAddress;
        }
        //set cache associationView
        if (settings.outputFile != null)
        {
            try
            {
                settings.client.Objects.Clear();
                settings.client.Objects.AddRange(GXDLMSObjectCollection.Load(settings.outputFile));
                isAssociationViewReaded = true;
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
        }
    }

    public void Dispose()
    {
        Console.WriteLine("DLMS Client disposing...");
        reader.Close();
        Console.WriteLine("DLMS Client disposed");
    }

    public void ExportMeterCertificate()
    {
        //Export client and server certificates from the meter.
        if (!string.IsNullOrEmpty(settings.ExportSecuritySetupLN))
        {
            reader.ExportMeterCertificates(settings.ExportSecuritySetupLN);
        }
    }
    public void GenerateCertificate()
    {
        //Generate new client and server certificates and import them to the server.
        if (!string.IsNullOrEmpty(settings.GenerateSecuritySetupLN))
        {
            reader.GenerateCertificates(settings.GenerateSecuritySetupLN);
        }

    }
    public IEnumerable<object> ReadObject(List<KeyValuePair<string, int>> readObjects, DlmsReadObjectFilterDto filter)
    {
        InitializeConnection(filter.SkipGettingAssociationView);

        //if (settings.readObjects.Count != 0)
        if (readObjects.Count != 0)
        {
            foreach (KeyValuePair<string, int> it in readObjects)
            {
                var value = InternalReadObject(it, filter);
                if (value != null) yield return value;
            }
            if (settings.outputFile != null)
            {
                try
                {
                    settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
                }
                catch (Exception)
                {
                    //It's OK if this fails.
                }
            }
        }
    }

    public object ReadObject(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter)
    {
        InitializeConnection(filter.SkipGettingAssociationView);

        var result = InternalReadObject(it, filter);

        if (settings.outputFile != null)
        {
            try
            {
                settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
        }
        return result;
    }

    public ProfileGenericValuesDto ReadProfileGenericValue(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter)
    {
        InitializeConnection(false);

        var gxObject = settings.client.Objects.FindByLN(ObjectType.None, it.Key) ??
            new GXDLMSObject { LogicalName = it.Key };

        if (gxObject is not GXDLMSProfileGeneric gxpg)
            throw new StatusCodeException(400, "Invalid profile generic logical name");

        reader.GetProfileGeneric(gxpg, filter);

        var result = new ProfileGenericValuesDto();
        if (gxpg.Buffer.Count == 0) return result;
        var assignMaps = GetAssignMaps(result, gxpg.CaptureObjects.Select(p => $"{p.Key.Description} ({p.Key.LogicalName})"), gxpg.Buffer.First());
        var span = CollectionsMarshal.AsSpan(gxpg.Buffer);

        foreach (var buffer in span.Slice(1))
            for (var i = 0; i < buffer.Length; i++) assignMaps[i](buffer[i]);

        if (settings.outputFile != null)
        {
            try
            {
                settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
        }
        return result;
    }

    private Action<object>[] GetAssignMaps(ProfileGenericValuesDto target, IEnumerable<string> key, IEnumerable<object> buffer)
    {
        var mapper = new Action<object>[buffer.Count()];
        var i = -1;
        foreach (var val in buffer)
        {
            ++i;
            if (i == 0)
                if (val is GXDateTime)
                {
                    DateTimeAssign(target, val); //first insert
                    mapper[i] = (obj) => DateTimeAssign(target, obj);
                    continue;
                }
                else throw new StatusCodeException(500, "Invalid object type, first column should be a clock type");

            if (val is int or uint or long or ulong or float or double or decimal)
            {
                var doubleList = new List<double>();
                target.DoubleValues[key.ElementAt(i)] = doubleList;

                NumberAssign(doubleList, val); //first insert
                mapper[i] = (obj) => NumberAssign(doubleList, obj);
                continue;
            }

            var valueList = new List<string>();
            target.StringValues[key.ElementAt(i)] = valueList;

            StringAssign(valueList, val); //first insert
            mapper[i] = (obj) => StringAssign(valueList, obj);
        }
        return mapper;
    }
    private void StringAssign(List<string> target, object value)
    {
        target.Add(value.ToString() ?? string.Empty);
    }
    private void NumberAssign(List<double> target, object value)
    {
        double result = 0;

        switch (value)
        {
            case double d:
                result = d;
                break;
            case int i:
                result = i;
                break;
            case long l:
                result = l;
                break;
            case float f:
                result = f;
                break;
            case decimal m:
                result = (double)m;
                break;
            case uint i:
                result = i;
                break;
            case ulong i:
                result = i;
                break;
            case string s when double.TryParse(s, out var parsed):
                value = parsed;
                break;
            default:
                throw new InvalidCastException("Not a numeric value");
        }
        target.Add(result);
    }
    private void DateTimeAssign(ProfileGenericValuesDto target, object value)
    {
        if (value is GXDateTime time) target.Times.Add(time.Value.UtcDateTime);
        else throw new StatusCodeException(500, "first index type should be a clock");
    }

    private void InitializeConnection(bool skipGettingAssociationView)
    {
        if (!isInitialized)
        {
            reader.InitializeConnection();
            if (!skipGettingAssociationView && !isAssociationViewReaded && reader.GetAssociationView(settings.outputFile))
            {
                reader.GetProfileGenericColumns();
                reader.GetScalersAndUnits();
                if (settings.outputFile != null)
                {
                    try
                    {
                        settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
                    }
                    catch (Exception)
                    {
                        //It's OK if this fails.
                    }
                }
            }
            isInitialized = true;
        }
    }
    private object InternalReadObject(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter)
    {
        var gxObject = settings.client.Objects.FindByLN(ObjectType.None, it.Key) ??
            new GXDLMSObject { LogicalName = it.Key };

        if (gxObject is GXDLMSProfileGeneric gxpg)
        {
            if (it.Value == 2)
            {
                reader.GetProfileGeneric(gxpg, filter);

                var result = gxpg.CaptureObjects.Select((p, index) => new
                {
                    p.Key.Description,
                    values = gxpg.Buffer.Select(buff => buff[index]).ToList()
                }).ToDictionary(p => p.Description, p => p.values);
                return result;
            }
            if (it.Value == 3)
            {
                var result = new Dictionary<string, object>
                {
                    ["value"] = gxpg.CaptureObjects.Select(p => p.Key.Description)
                };
                return result;
            }
        }

        object val = reader.Read(gxObject, it.Value, filter.SkipGettingAssociationView);

        if (val is GXDLMSClock gclk)
        {
            return gclk.Time;
        }

        return val;
    }
}
