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
using DLMS.Client.GXMedia.Mqtt;
using DlmsMqttExecutor.Application.DTOs.Dlms;
using DlmsMqttExecutor.Application.Exceptions;
using DlmsMqttExecutor.Application.Interfaces;
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
        if (ret != 0) throw new ArgumentNullException($"{nameof(args)}", "Argument can't be null.");
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

        settings.media.Open();
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
    public void SetDisconnectControl(bool value, DlmsReadObjectFilterDto filter, string? ln = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ln)) ln = "0.0.96.3.10.255"; //default ln
        var control = GetAndReadObject<GXDLMSDisconnectControl>(new(ln, 4), filter, ObjectType.DisconnectControl, cancellationToken);
        if (control is not GXDLMSDisconnectControl gxDC)
            throw new StatusCodeException(400, $"Invalid disconnect control logical name: {ln}");
        var packet = value ? gxDC.RemoteReconnect(settings.client) : gxDC.RemoteDisconnect(settings.client);
        var reply = new GXReplyData();

        cancellationToken.ThrowIfCancellationRequested();
        var isRejected = reader.ReadDataBlock(packet, reply);

        if (isRejected) throw new StatusCodeException(400, "Disconnect control execution failed");
    }
    public void ExecuteScript(string ln, int scriptId, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default)
    {
        var gxScript = GetAndReadObject<GXDLMSScriptTable>(new(ln, 2), filter, ObjectType.ScriptTable, cancellationToken);
        var script = gxScript.Scripts.FirstOrDefault(p => p.Id == scriptId) ??
            throw new StatusCodeException(400, $"Script ID: {scriptId} not found");
        var packet = gxScript.Execute(settings.client, script);
        var reply = new GXReplyData();

        foreach (var pack in packet)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reader.ReadDLMSPacket(pack, reply);
        }
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
    public IEnumerable<object> ReadObject(
        List<KeyValuePair<string, int>> readObjects, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default)
    {
        InitializeConnection(filter.SkipGettingAssociationView, cancellationToken);

        //if (settings.readObjects.Count != 0)
        if (readObjects.Count != 0)
        {
            foreach (KeyValuePair<string, int> it in readObjects)
            {
                var value = InternalReadObject(it, filter, cancellationToken: cancellationToken);
                if (value != null) yield return value;
            }
            //if (settings.outputFile != null)
            //{
            //    try
            //    {
            //        settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
            //    }
            //    catch (Exception)
            //    {
            //        //It's OK if this fails.
            //    }
            //}
        }
    }
    /// <summary>
    /// Read value of target object
    /// </summary>
    /// <param name="it"></param>
    /// <param name="filter"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Value object</returns>
    public object ReadObject(KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default)
    {
        InitializeConnection(filter.SkipGettingAssociationView, cancellationToken);

        var result = InternalReadObject(it, filter, cancellationToken: cancellationToken);
        //if (settings.outputFile != null)
        //{
        //    try
        //    {
        //        settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
        //    }
        //    catch (Exception)
        //    {
        //        //It's OK if this fails.
        //    }
        //}
        return result;
    }
    /// <summary>
    /// Get Target object and read index
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="it"></param>
    /// <param name="filter"></param>
    /// <param name="objectType"></param>
    /// <returns>GXDLMSObject as T</returns>
    /// <exception cref="StatusCodeException"></exception>
    public T GetAndReadObject<T>(
        KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter, ObjectType objectType, CancellationToken cancellationToken = default)
        where T : GXDLMSObject
    {
        InitializeConnection(filter.SkipGettingAssociationView, cancellationToken);
        var gxObject = settings.client.Objects.FindByLN(objectType, it.Key) ?? reader.GetObjectManually(it.Key);
        if (gxObject is not T gxTarget)
            throw new StatusCodeException(400, $"Invalid gx object type: {gxObject.ObjectType}");

        var _ = InternalReadObject(it, filter, gxTarget, cancellationToken);

        //if (settings.outputFile != null)
        //{
        //    try
        //    {
        //        settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
        //    }
        //    catch (Exception)
        //    {
        //        //It's OK if this fails.
        //    }
        //}
        return gxTarget;
    }

    public ProfileGenericValuesDto ReadProfileGenericValue(
        KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter, CancellationToken cancellationToken = default)
    {
        InitializeConnection(filter.SkipGettingAssociationView, cancellationToken);

        var gxObject = settings.client.Objects.FindByLN(ObjectType.None, it.Key) ??
            GetAndReadObject<GXDLMSProfileGeneric>(it, filter, ObjectType.ProfileGeneric);

        if (gxObject is not GXDLMSProfileGeneric gxpg)
            throw new StatusCodeException(400, $"Invalid profile generic logical name: {it.Key}");

        cancellationToken.ThrowIfCancellationRequested();
        reader.ReadProfileGeneric(gxpg, filter);

        var result = new ProfileGenericValuesDto();
        if (gxpg.Buffer.Count == 0) return result;
        var assignMaps = GetAssignMaps(result, gxpg.CaptureObjects.Select(p => $"{p.Key.Description} ({p.Key.LogicalName})"), gxpg.Buffer.First());

        foreach (var buffer in gxpg.Buffer)
        {
            var row = new ProfileGenericRowDto();
            for (var i = 0; i < buffer.Length; i++) assignMaps[i](row, buffer[i]);
            result.Rows.Add(row);
        }

        //if (settings.outputFile != null)
        //{
        //    try
        //    {
        //        settings.client.Objects.Save(settings.outputFile, new GXXmlWriterSettings() { UseMeterTime = true, IgnoreDefaultValues = false });
        //    }
        //    catch (Exception)
        //    {
        //        //It's OK if this fails.
        //    }
        //}
        return result;
    }

    private static Action<ProfileGenericRowDto, object>[] GetAssignMaps(
        ProfileGenericValuesDto target, IEnumerable<string> key, IEnumerable<object> buffer)
    {
        var mapper = new Action<ProfileGenericRowDto, object>[buffer.Count()];
        var i = -1;
        foreach (var val in buffer)
        {
            ++i;
            if (i == 0)
            {
                if (val is not GXDateTime) throw new StatusCodeException(500, "Invalid object type, first column should be a clock type");
                mapper[i] = DateTimeAssign;
                continue;
            }

            if (val is int or uint or long or ulong or float or double or decimal)
            {
                target.DobleSchema.Add(key.ElementAt(i)); //add schema
                mapper[i] = NumberAssign;
                continue;
            }

            target.StringSchema.Add(key.ElementAt(i)); //add schema
            mapper[i] = StringAssign;
        }
        return mapper;
    }
    private static void StringAssign(ProfileGenericRowDto target, object value)
    {
        target.StringValues.Add(value.ToString() ?? string.Empty);
    }
    private static void NumberAssign(ProfileGenericRowDto target, object value)
    {
        var result = value switch
        {
            double d => d,
            int i => i,
            long l => l,
            float f => (double)f,
            decimal m => (double)m,
            uint i => i,
            ulong i => i,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => throw new InvalidCastException("Not a numeric value"),
        };
        target.DoubleValues.Add(result);
    }
    private static void DateTimeAssign(ProfileGenericRowDto target, object value)
    {
        if (value is GXDateTime time) target.Time = time.Value.UtcDateTime;
        else throw new StatusCodeException(500, "first index type should be a clock");
    }

    private void InitializeConnection(bool skipGettingAssociationView, CancellationToken cancellationToken = default)
    {
        if (!isInitialized)
        {
            reader.InitializeConnection(cancellationToken);
            isInitialized = true;
        }
        if (!skipGettingAssociationView && !isAssociationViewReaded && reader.GetAssociationView(settings.outputFile, cancellationToken))
        {
            reader.GetProfileGenericColumns(cancellationToken);
            reader.GetScalersAndUnits(cancellationToken);
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
            isAssociationViewReaded = true;
        }
    }
    private object InternalReadObject(
        KeyValuePair<string, int> it, DlmsReadObjectFilterDto filter, GXDLMSObject? gxObject = null, CancellationToken cancellationToken = default)
    {
        gxObject ??= settings.client.Objects.FindByLN(ObjectType.None, it.Key) ?? reader.GetObjectManually(it.Key);

        cancellationToken.ThrowIfCancellationRequested();
        if (gxObject is GXDLMSProfileGeneric gxpg)
        {
            var gxValue = reader.GetProfileGenericValue(gxpg, it.Value, filter);
            if (gxValue != null) return gxValue;
        }

        object val = reader.Read(gxObject, it.Value);

        return val;
    }
}
