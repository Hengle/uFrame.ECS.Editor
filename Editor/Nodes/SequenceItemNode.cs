using System.CodeDom;
using Invert.Core;
using Invert.Json;
using UnityEngine;

namespace Invert.uFrame.ECS
{
    using Invert.Core.GraphDesigner;
    using Invert.Data;
    using System.Collections.Generic;
    using System.Linq;

    public partial interface ISequenceItemConnectable : Invert.Core.GraphDesigner.IDiagramNodeItem, Invert.Core.GraphDesigner.IConnectable
    {
    }

    public class SequenceItemNode : SequenceItemNodeBase, ICodeOutput, IDataRecordRemoving
    {

        private string _variableName;

        public override bool AllowMultipleInputs
        {
            get { return false; }
        }

        public override bool AllowMultipleOutputs
        {
            get { return false; }
        }

        public override Color Color
        {
            get { return Color.blue; }
        }

        public IVariableContextProvider Left
        {
            get
            {
                var r = this.InputFrom<IVariableContextProvider>();
                return r;
            }
        }

        public IEnumerable<IVariableContextProvider> LeftNodes
        {
            get
            {
                var left = Left;
                while (left != null)
                {
                    yield return left;
                    left = left.Left;
                }
            }
        }

        public Breakpoint BreakPoint
        {
            get { return Repository.All<Breakpoint>().FirstOrDefault(p => p.ForIdentifier == this.Identifier); }
        }

        public SequenceItemNode Right
        {
            get { return this.OutputTo<SequenceItemNode>(); }
        }

        public IEnumerable<IVariableContextProvider> RightNodes
        {
            get
            {
                var right = Right;
                while (right != null)
                {
                    yield return right;
                    right = right.Right;
                }
            }
        }

        [JsonProperty, InspectorProperty]
        public string VariableName
        {
            get {
                return _variableName ?? (_variableName = VariableNameProvider.GetNewVariableName(this.GetType().Name));
            }
            set { this.Changed("VariableName", ref _variableName, value); }
        }

        public IVariableNameProvider VariableNameProvider
        {
            get { return Graph as IVariableNameProvider; }
        }

        public virtual bool IsAsync { get { return false; } }

        public IEnumerable<IContextVariable> GetAllContextVariables()
        {
            var left = Left;
            if (left != null)
            {
                foreach (var contextVar in left.GetAllContextVariables())
                {
                    yield return contextVar;
                }
            }
            foreach (var item in GetContextVariables())
            {
                yield return item;
            }
        }

        public virtual IEnumerable<IContextVariable> GetContextVariables()
        {
            yield break;
        }

        public override void RecordRemoved(IDataRecord record)
        {
            base.RecordRemoved(record);
            var container = this.Container();
            if (container == null || container.Identifier == record.Identifier)
            {
                Repository.Remove(this);
            }
        }

        public static string LastSequenceItemId = string.Empty;

        public virtual void WriteDebugInfo(TemplateContext ctx)
        {
            if (DebugSystem.IsDebugMode)
            {
                ctx._("while (this.DebugInfo(\"{0}\",\"{1}\", this) == 1) yield return null", LastSequenceItemId, this.Identifier);
                LastSequenceItemId = this.Identifier;
            }
          
        }
        public virtual void WriteCode(IHandlerNodeVisitor visitor, TemplateContext ctx)
        {
    
            

        }

        public void OutputVariables(TemplateContext ctx)
        {
            foreach (var item in GraphItems.OfType<IConnectable>())
            {
                var decl = item.InputFrom<VariableNode>();
                if (decl == null) continue;
                var field = decl.GetFieldStatement();
                bool found = false;
                foreach (var f in ctx.CurrentDeclaration.Members.OfType<CodeMemberField>())
                {
                    if (f.Name == field.Name)
                    {
                        found = true;
                    }
                }
                if (!found)
                ctx.CurrentDeclaration.Members.Add(field);
            }
        }

        public virtual void RecordRemoving(IDataRecord record)
        {
            //if (record == this)
            //{
            //    foreach (var item in GraphItems)
            //    {
            //        Repository.Remove(item);
            //    }
            //}
        }

        public void WriteActionInputs(TemplateContext _)
        {
            foreach (var output in this.GraphItems.OfType<IActionIn>())
            {
                WriteActionInput(_, output);
            }
        }

        private void WriteActionInput(TemplateContext _, IActionIn output)
        {
            if (output.ActionFieldInfo != null && output.ActionFieldInfo.IsReturn) return;
            _._("{0} = {1}.{2}", output.VariableName, VariableName, output.Name);
            var variableReference = output.InputFrom<IContextVariable>();
            if (variableReference != null)
                _.CurrentStatements.Add(new CodeAssignStatement(new CodeSnippetExpression(variableReference.VariableName),
                    new CodeSnippetExpression(output.VariableName)));

            //var actionIn = output.OutputTo<IActionIn>();
            //if (actionIn != null)
            //{
            //    _.CurrentStatements.Add(new CodeAssignStatement(
            //             new CodeSnippetExpression(output.VariableName), new CodeSnippetExpression(actionIn.VariableName)
               
            //        ));
            //}
        }

        public void WriteActionOutputs(TemplateContext _)
        {
            foreach (var output in this.GraphItems.OfType<ActionOut>())
            {
                WriteActionOutput(_, output);
            }
        }

        private void WriteActionOutput(TemplateContext _, IActionOut output)
        {
            if (output.ActionFieldInfo != null && output.ActionFieldInfo.IsReturn) return;
            _._("{0} = {1}.{2}", output.VariableName, VariableName, output.Name);
            var variableReference = output.OutputTo<IContextVariable>();
            if (variableReference != null)
                _.CurrentStatements.Add(new CodeAssignStatement(new CodeSnippetExpression(variableReference.VariableName),
                    new CodeSnippetExpression(output.VariableName)));
            var actionIn = output.OutputTo<IActionIn>();
            if (actionIn != null)
            {
                _.CurrentStatements.Add(new CodeAssignStatement(
                    new CodeSnippetExpression(actionIn.VariableName),
                    new CodeSnippetExpression(output.VariableName)));
            }
        }
    }
}