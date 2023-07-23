using Newtonsoft.Json;
using PlainSequencer.Logging;
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
            dynamic responseModel = null;
            var item_quantity_cap = item.SequenceItem?.take_only_n;
            try
            {
                bool resolved = false;
                try 
                {
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
            var content = JsonConvert.SerializeObject(model);

            if (model == null) return null;

            try {
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

        public static async Task<T> FailableRun<T>(ILogSequence logProgress, ISequenceItemAction siAct, Func<Task<T>> f)
        {
            logProgress.StartItem((SequenceItemAbstract)siAct);
            siAct.Started = DateTime.Now;
            var sir = (ISequenceItemResult)siAct;
            try { return await f(); } 
            catch (Exception e) 
            { 
                sir.Exception = e;
                sir.Fail(e);
                return default(T);
            }
            finally
            {
                siAct.Finished = DateTime.Now;
                logProgress.FinishedItem((SequenceItemAbstract)siAct);

                var siStract = (SequenceItemAbstract)siAct;
                if (sir.IsFail)
                {
                    if (sir.Exception != null)
                        logProgress.Fail(siStract, sir.Exception);
                    else
                        logProgress.Fail(siStract, sir.FailMessage);

                    if (!siAct.SequenceItem.is_continue_on_failure)
                        sir.NullResult();
                    else if (sir.ActionResult is null)
                        sir.BlankResult();
                }
            }
        }
    }
}
