using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITraining_3_3_1
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));

            using (Transaction ts = new Transaction(doc, "Добавить параметр"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Длина с запасом",
                    categorySet, BuiltInParameterGroup.PG_LENGTH, true); //Требуется создать общий параметр "Длина с запасом"
                ts.Commit();
            }

            TaskDialog.Show("Выполнено", "Параметр создан");

            IList<Reference> selectedPipesRefList = uidoc.Selection.PickObjects(ObjectType.Element, new PipesFilter(), "Выберите трубы");

            foreach (var selectedPipeRef in selectedPipesRefList)
            {
                Pipe pipe = doc.GetElement(selectedPipeRef) as Pipe;
                Parameter length = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                if (length.StorageType==StorageType.Double)
                {
                    using (Transaction ts1 = new Transaction(doc, "Задать параметр"))
                    {
                        ts1.Start();
                        Parameter lenghthAddParameter = pipe.LookupParameter("Длина с запасом");
                        double lenghthAdd = UnitUtils.ConvertFromInternalUnits(length.AsDouble()*1.1, UnitTypeId.Meters);
                        lenghthAddParameter.Set(lenghthAdd);
                        ts1.Commit();
                    }
                }
            }
            TaskDialog.Show("Выполнено", "Параметр записан");

            return Result.Succeeded;
        }
        private void CreateSharedParameter(Application application,
            Document doc, string parameterName, CategorySet categorySet,
            BuiltInParameterGroup builthInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builthInParameterGroup);
        }
    }
}
