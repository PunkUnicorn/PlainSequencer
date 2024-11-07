using Newtonsoft.Json;
using PlainSequencer.Logging;
using PlainSequencer.Script;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace PlainSequencer.SequenceItemActions
{
    public static class SequenceItemStatic
    {
        public static dynamic GetResponseItems(ILogSequence logSequence, SequenceItemAbstract item, string content)
        {
            if ((content?.Length ?? -1) == 0)
                return content;

            dynamic responseModel = null;
            var item_quantity_cap = item?.SequenceItem?.take_only_n;
            try
            {
                bool resolved = false;
                try 
                {
                    //responseModel = JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(content);
                    responseModel = JsonConvert.DeserializeObject<List<ExpandoObject>>(content);
                    resolved = true;
                } catch { }

                if (!resolved) try
                    {
                        responseModel = JsonConvert.DeserializeObject<List<object>>(content);
                        resolved = true;
                    } catch { }

                if (!resolved) try
                {
                    responseModel = JsonConvert.DeserializeObject<IDictionary<string, object>>(content);
                    responseModel = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(responseModel));
                    resolved = true;
                }
                catch { }

                if (!resolved)
                {
                    responseModel = JsonConvert.DeserializeObject<object>(content);
                }
                else if (item_quantity_cap != null)
                {
                    logSequence?.Progress(item, $" taking the first {item_quantity_cap} results only...", SequenceProgressLogLevel.Diagnostic);
                    responseModel = ((IEnumerable<object>)responseModel).Take(item_quantity_cap.Value).ToList();
                }
            }
            catch
            {
                responseModel = content;
            }
            return responseModel;
        }

        public static object Clone(object model)
        {
            return model;
            //return SequenceItemStatic.GetResponseItems(null, null, model);
            if (model == null) return null;
            if (((model as string)?.Length ?? -1) == 0) return "";

            var content = JsonConvert.SerializeObject(model);
            try
            {
                bool resolved = false;
                dynamic clone = null;
                try
                {
                    clone = (List<IDictionary<string, object>>)JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(content);
                    resolved = true;
                }
                catch { }

                if (!resolved) try
                {
                    clone = (List<object>)JsonConvert.DeserializeObject<List<object>>(content);
                    resolved = true;
                }
                catch { }

                if (!resolved)
                    clone = (object)JsonConvert.DeserializeObject<object>(content);                        

                return clone;
            }
            catch 
            { 
                if (model is string strModel)
                    return new String(strModel.ToArray()); 

                throw;
            }
        }

        public static async Task DoInlineSaveAsync(object content, object scribanModel, Save save, IEnumerable<Save> saves)
        {
            //change it so it dump saves the bytes, see if that works?
            if (save is not null)
                await SequenceItemSave.DoSaveAsync(save, content, scribanModel);

            if (saves is not null)
                foreach (var s in saves)
                    await SequenceItemSave.DoSaveAsync(s, content, scribanModel);
        }

        public static async Task<T> FailableRun<T>(ILogSequence logProgress, ISequenceItemActionRun siRun, Func<Task<T>> f)
        {
            var siAct = (ISequenceItemAction)siRun;
            logProgress.StartItem(siAct);
            siRun.Started = DateTime.Now;
            try { return await f(); } 
            catch (Exception e) 
            {
                siRun.Exception = e;
                siRun.Fail(e);
                return default(T);
            }
            finally
            {
                siRun.Finished = DateTime.Now;
                logProgress.FinishedItem(siAct);

                var sir = (ISequenceItemResult)siRun;
                if (sir.IsFail)
                {
                    if (sir.Exception != null)
                        logProgress.Fail(siAct, sir.Exception);
                    else
                        logProgress.Fail(siAct, sir.FailMessage);

                    if (!siAct.SequenceItem.is_continue_on_failure)
                        siRun.NullResult();
                    else if (sir.ActionResult is null)
                        siRun.BlankResult();
                }
            }
        }


        // https://stackoverflow.com/questions/472906/how-do-i-get-a-consistent-byte-representation-of-strings-in-c-sharp-without-manu
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        // Do NOT use on arbitrary bytes; only use on GetBytes's output on the SAME system
        public static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}
