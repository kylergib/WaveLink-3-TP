using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace WaveLinkSDK.Interfaces;
internal interface IWaveLinkClient
{
    public string Url { get; set; }
    public int Port { get; set; } // 28196
    public void Connect();
    public void Close();
    public void SetUpEvents();
    public void Send(string message);
    public void OnMessage(object? sender, EventArgs e);
    public void OnError(object? sender, EventArgs e);
    public void OnClose(object? sender, EventArgs e);

}
