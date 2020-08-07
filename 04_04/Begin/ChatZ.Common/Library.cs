using Newtonsoft.Json;
using System.Text;

namespace ChatZ.Common
{
  internal static class Library
  {
    internal static int HashCombine(int num1, int num2)
    {
      var num = (uint)(num1 << 5 | (int)((uint)num1 >> 27));
      return (int)(num + (uint)num1 ^ (uint)num2);
    }

    internal static byte[] Encode<T>(T value) 
      => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));

    internal static bool TryDecode<T>(byte[] value, ref T msg)
    {
      try
      {
        var raw = Encoding.UTF8.GetString(value);
        msg = JsonConvert.DeserializeAnonymousType(raw, msg);
        return true;
      }
      catch
      {
        msg = default(T);
        return false;
      }
    }

    internal static bool TryGetString(byte[] value, out string output)
    {
      try
      {
        output = Encoding.UTF8.GetString(value);
        return true;
      }
      catch
      {
        output = null;
        return false;
      }
    }
  }
}
