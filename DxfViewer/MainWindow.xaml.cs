using DxfViewer.Controls;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DxfViewer
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            // your DXF file name
            string file = "doc.dxf";

            // load file
            DxfDocument loaded = DxfDocument.Load(file);
            IList<netDxf.Entities.EntityObject> entities = new List<netDxf.Entities.EntityObject>();
            netDxf.Entities.EntityObject[] entityObjects = loaded.Entities.All.ToArray();
            for (int i = 0; i < entityObjects.Length; i++)
            {
                Explode(entityObjects[i], entities);
            }

            for (int i = 0; i < entities.Count; i++)
            {
                DxfEntityObject dxfEntityObject = new DxfEntityObject();
                dxfEntityObject.EntityObject = entities[i];
                m_canvas.Children.Add(dxfEntityObject);
            }
        }

        private void Explode(netDxf.Entities.EntityObject entityObject, IList<netDxf.Entities.EntityObject> entities)
        {
            if (entityObject.Type == EntityType.Insert)
            {
                List<netDxf.Entities.EntityObject> exploded = ((netDxf.Entities.Insert)entityObject).Explode();

                for (int i = 0; i < exploded.Count; i++)
                {
                    var item = exploded[i];

                    if (item.Type == EntityType.Insert)
                    {
                        Explode((netDxf.Entities.EntityObject)item, entities);
                    }
                    else
                    {
                        entities.Add(item);
                        
                    }
                }
                
            }

            else if (entityObject.Type == EntityType.Dimension)
            {
                List<netDxf.Entities.EntityObject> listEntities = ((netDxf.Entities.Dimension)entityObject).Block.Entities.ToList();

                for (int i = 0; i < listEntities.Count; i++)
                {
                    var item = listEntities[i];

                    if (item.Type == EntityType.Insert)
                    {
                        Explode((netDxf.Entities.EntityObject)item, entities);
                    }
                    else
                    {
                        entities.Add(item);

                    }
                }

            }
            else
            {
                entities.Add(entityObject); 
            }
        }
    }
}
