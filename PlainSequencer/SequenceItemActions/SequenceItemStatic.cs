using Newtonsoft.Json;
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
        public static dynamic GetResponseItems(SequenceItem sequenceItem, string content)
        {
            dynamic responseModel = null;
            var item_quantity_cap = sequenceItem?.take_only_n;
            try
            {
                //if (item_quantity_cap != null)
                //    state.ProgressLog?.Progress($" taking the first {item_quantity_cap} results only...");

                //if (item_quantity_cap != null)
                //    responseModel = JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(content).Take(item_quantity_cap.Value).ToList();
                //else
                //{

                bool resolved = false;
                try 
                {
                    //content = "[{\"Id\":\"00000001\"}]";
                    responseModel = JsonConvert.DeserializeObject<List<ExpandoObject>>(content);
                    //var look = JsonConvert.SerializeObject(responseModel, Formatting.Indented);
                    resolved = true;
                } catch { }

                if (!resolved) try 
                { 
                    responseModel = JsonConvert.DeserializeObject<List<object>>(content);
                    resolved = true;
                } catch { }

                //if (!resolved) try
                //{
                //    responseModel = JsonConvert.DeserializeObject<IDictionary<string, object>>(content);
                //    resolved = true;
                //}
                //catch { }

                if (!resolved)
                {
                    responseModel = JsonConvert.DeserializeObject<object>(content);
                }
                else if (item_quantity_cap != null)
                {
                    //state.ProgressLog?.Progress(this, $" taking the first {item_quantity_cap} results only...");
                    responseModel = ((IEnumerable<object>)responseModel).Take(item_quantity_cap.Value).ToList();
                }
                //}
            }
            catch
            {
                responseModel = content;
            }
            return responseModel;
        }

        public static dynamic GetResponseItems(SequenceItem sequenceItem, List<dynamic> content)
        {
            dynamic responseModel;
            var item_quantity_cap = sequenceItem?.take_only_n;
            try
            {
                //if (item_quantity_cap != null)
                //    state.ProgressLog?.Progress($" taking the first {item_quantity_cap} results only...");

                responseModel = item_quantity_cap != null
                    ? content.Take(item_quantity_cap.Value).ToList()
                    : content;
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
                {
                    //clone = (JsonConvert.DeserializeObject<ExpandoObject>(content) as IDictionary<string, object>)
                    //    .ToDictionary(k => k.Key, v => v.Value);
                    clone = (object)JsonConvert.DeserializeObject<object>(content);                        
                }
                return clone;
            }
            catch 
            { 
                if (model is string strModel)
                    return new String(strModel.ToArray()); 

                throw;
                //throw new InvalidCastException(content);
                ////return model;
            }
        }

        public static async Task<T> FailableRun<T>(ISequenceItemAction sia, Func<Task<T>> f)
        { 
            try { return await f(); } 
            catch (Exception e) 
            { 
                var sir = (ISequenceItemResult) sia;
                sir.Exception = e;
                sir.Fail(e);//.IsFail = true; 
            }

            return default(T);
        }
    }
}
