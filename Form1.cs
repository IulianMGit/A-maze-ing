using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Collections;
using System.Diagnostics;
using WindowsFormsApplication1;

namespace WindowsFormsApplication1
{


  public partial class Form1 : Form
  {
    Maze maze;
    Bitmap MazeBitmap;
    public Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      doLayout();
    }
    public void doLayout()
    {
      Panel2.Top = 100;
      Panel2.Left = 0;
      Panel2.Height = this.ClientRectangle.Height - Panel2.Top;
      Panel2.Width = this.ClientRectangle.Width;
      Panel2.BorderStyle = BorderStyle.FixedSingle;
    }
    private void Button1_Click(object sender, EventArgs e)
    {
      maze = new Maze(Convert.ToInt32(nudRows.Value), Convert.ToInt32(nudCols.Value), Convert.ToInt32(nudWidth.Value), Convert.ToInt32(nudHeight.Value));
      maze.MazeComplete += (Image m) =>
      {
        Panel1.BackgroundImage = m;
        Panel1.BackgroundImageLayout = ImageLayout.None;
        Panel1.Width = m.Width;
        Panel1.Height = m.Height;
        //maze.PrintMaze()
      };
      maze.Generate();
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
      doLayout();
    }

    private void button2_Click(object sender, EventArgs e)
    {
      //var content = this.maze.GetContentAsMatrix();
      var Q = new Dictionary<QState, Dictionary<QState, double>>();
      var Nsa = new Dictionary<QState, Dictionary<QState, double>>();

      foreach (var cell in maze.cells.Values)
      {
        maze.ClearCells(cell.id);
        Dictionary<QState, double> Qcell;
        if (!Q.ContainsKey(cell))
        {
          Qcell = new Dictionary<QState, double>();
          Q.Add(cell, Qcell);

          var neighs = cell.GetAvailableStates();

          foreach (var neigh in neighs)
          {
            Qcell.Add((Cell)neigh, 0);
          }

          Qcell.Add((Cell)cell, double.MinValue);
        }
      }

      foreach (var cell in maze.cells.Values)
      {
        Dictionary<QState, double> Qcell;
        if (!Nsa.ContainsKey(cell))
        {
          Qcell = new Dictionary<QState, double>();
          Nsa.Add(cell, Qcell);

          foreach (var neigh in maze.cells.Values)
          {
            Qcell.Add((Cell)neigh, 1);
          }
        }
      }



      QLearning Qlearn = new QLearning(maze.cells.Values.Select(x => (QState)x).ToList(), Q, Nsa);
      Qlearn.Train(maze.cells.Last().Value, int.Parse(this.textBox3.Text), double.Parse(this.textBox1.Text), double.Parse(this.textBox2.Text));

      var road = Qlearn.Walk(maze.cells.First().Value, maze.cells.Last().Value);

      foreach (var way in road)
      {
        var cell = way as Cell;

        maze.VisitCell(cell.id);
      }
    }

    private void Panel1_Click(object sender, EventArgs e)
    {
      var clickEvent = e as MouseEventArgs;
      var cWidth = Convert.ToInt32(nudWidth.Value);
      var cHeight = Convert.ToInt32(nudHeight.Value);

      int cellX = clickEvent.X / cHeight;
      int cellY = clickEvent.Y / cWidth;
      string key = "c" + cellX + "r" + cellY;

      maze.ChangeCellState(key);
    }
  }
}

public struct QCell
{
  public short type;
  public short N;
  public short S;
  public short E;
  public short W;

  public QCell(short _type, short _N, short _S, short _E, short _W)
  {
    type = _type;
    N = _N;
    S = _S;
    E = _E;
    W = _W;
  }
}

public class Maze : Control
{
  int Rows;
  int Columns;
  int cellWidth;
  int cellHeight;
  public Dictionary<string, Cell> cells = new Dictionary<string, Cell>();

  Stack<Cell> stack = new Stack<Cell>();
  Bitmap bitmap;

  public Image MazeImage;
  public event MazeCompleteEventHandler MazeComplete;
  public delegate void MazeCompleteEventHandler(Image Maze);
  private event CallCompleteEventHandler CallComplete;
  private delegate void CallCompleteEventHandler(Image Maze);

  public QCell[,] GetContentAsMatrix()
  {
    var result = new QCell[Rows, Columns];

    foreach (var cell in cells.Values)
    {
      result[cell.Row, cell.Column] = new QCell((short)cell.state, (short)(cell.NorthWall ? 1 : 0),
        (short)(cell.SouthWall ? 1 : 0), (short)(cell.EastWall ? 1 : 0), (short)(cell.WestWall ? 1 : 0));
    }

    return result;
  }

  public new Rectangle Bounds
  {
    get
    {
      Rectangle rect = new Rectangle(0, 0, Width, Height);
      return rect;
    }
  }
  private System.Drawing.Printing.PrintDocument withEventsField_printDoc = new System.Drawing.Printing.PrintDocument();
  public System.Drawing.Printing.PrintDocument printDoc
  {
    get { return withEventsField_printDoc; }
    set
    {
      if (withEventsField_printDoc != null)
      {
        withEventsField_printDoc.PrintPage -= PrintImage;
      }
      withEventsField_printDoc = value;
      if (withEventsField_printDoc != null)
      {
        withEventsField_printDoc.PrintPage += PrintImage;
      }
    }
  }
  private void PrintImage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
  {
    List<string> nonprinters = new string[] { "Send To OneNote 2013", "PDFCreator", "PDF Architect 4", "Microsoft XPS Document Writer", "Microsoft Print to PDF", "Fax", "-" }.ToList();
    string printerName = "none";
    foreach (string a in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
    {
      if (nonprinters.IndexOf(a) > -1)
        continue;
      printerName = a;
    }
    if (printerName == "none")
      return;
    printDoc.PrinterSettings.PrinterName = printerName;
    int imageLeft = Convert.ToInt32(e.PageBounds.Width / 2) - Convert.ToInt32(MazeImage.Width / 2);
    int imageTop = Convert.ToInt32(e.PageBounds.Height / 2) - Convert.ToInt32(MazeImage.Height / 2);
    e.Graphics.DrawImage(MazeImage, imageLeft, imageTop);
  }
  public void PrintMaze()
  {
    printDoc.Print();
  }
  public void Generate()
  {
    int c = 0;
    int r = 0;
    for (int y = 0; y < Height - 1; y += cellHeight)
    {
      for (int x = 0; x < Width - 1; x += cellWidth)
      {
        Cell cell = new Cell(new Point(x, y), new Size(cellWidth, cellHeight), ref cells, r, c, (Rows - 1), (Columns - 1));
        c += 1;
      }
      c = 0;
      r += 1;
    }
    System.Threading.Thread thread = new System.Threading.Thread(Dig);
    thread.Start();
  }
  public void VisitCell(string cellID)
  {
    bitmap = new Bitmap(bitmap);
    using (Graphics g = Graphics.FromImage(bitmap))
    {
      Cell cell = cells[cellID];
      cell.Visit(g);
    }

    if (CallComplete != null)
    {
      CallComplete(bitmap);
    }
  }

  public void ClearCells(string cellID)
  {
    bitmap = new Bitmap(bitmap);
    using (Graphics g = Graphics.FromImage(bitmap))
    {
      Cell cell = cells[cellID];
      cell.Clear(g);
    }

    if (CallComplete != null)
    {
      CallComplete(bitmap);
    }
  }

  public void ChangeCellState(string cellID)
  {
    bitmap = new Bitmap(bitmap);
    using (Graphics g = Graphics.FromImage(bitmap))
    {

      Cell cell = cells[cellID];
      cell.ChangeState();
      cell.draw(g);
    }

    if (CallComplete != null)
    {
      CallComplete(bitmap);
    }
  }

  private void Dig()
  {
    int r = 0;
    int c = 0;
    string key = "c" + 5 + "r" + 5;
    Cell startCell = cells[key];
    stack.Clear();
    startCell.Visited = true;
    while ((startCell != null))
    {
      startCell = startCell.Dig(ref stack);
      if (startCell != null)
      {
        startCell.Visited = true;
        startCell.Pen = Pens.Black;
      }
    }

    foreach (var item in cells)
    {
      item.Value.Visited = false;
    }
    if (true)//multiple exits
    {
      startCell = cells[key];
      stack.Clear();
      startCell.Visited = true;
      while ((startCell != null))
      {
        startCell = startCell.Dig(ref stack);
        if (startCell != null)
        {
          startCell.Visited = true;
          startCell.Pen = Pens.Black;
        }
      }
    }
    stack.Clear();
    bitmap = new Bitmap(Width, Height);
    using (Graphics g = Graphics.FromImage(bitmap))
    {
      g.Clear(Color.White);
      if (cells.Count > 0)
      {
        for (r = 0; r <= this.Rows - 1; r++)
        {
          for (c = 0; c <= this.Columns - 1; c++)
          {
            Cell cell = cells["c" + c + "r" + r];
            cell.draw(g);
          }
        }
      }
    }

    if (CallComplete != null)
    {
      CallComplete(bitmap);
    }
  }
  public delegate void dComplete(Image maze);
  private void Call_Complete(Image maze)
  {
    if (this.InvokeRequired)
    {
      this.Invoke(new dComplete(Call_Complete), maze);
    }
    else
    {
      if (MazeComplete != null)
      {
        MazeComplete(maze);
      }
    }
  }
  public Maze(int rows, int columns, int cellWidth, int cellHeight)
  {
    CallComplete += Call_Complete;
    this.Rows = rows;
    this.Columns = columns;
    this.cellWidth = cellWidth;
    this.cellHeight = cellHeight;
    this.Width = (this.Columns * this.cellWidth) + 1;
    this.Height = (this.Rows * this.cellHeight) + 1;
    this.CreateHandle();
  }
}

public enum State
{
  SimpleCell = 0,
  BonusCell = 1,
  EnemyCell = 2
}
public class Cell : QState
{
  public State state = 0;
  public SolidBrush color = new SolidBrush(Color.White);

  public bool NorthWall = true;
  public bool SouthWall = true;
  public bool WestWall = true;
  public bool EastWall = true;
  public string id;
  public Pen Pen = Pens.Black;
  public Rectangle Bounds;
  public Dictionary<string, Cell> Cells;
  public int Column;
  public int Row;
  public string NeighborNorthID;
  public string NeighborSouthID;
  public string NeighborEastID;
  public string NeighborWestID;
  public bool Visited = false;
  public Stack<Cell> Stack;

  public void ChangeState()
  {
    state = (State)(((int)state + 1) % 3);
    this.color = state == State.BonusCell ? (new SolidBrush(Color.Green)) : (state == State.EnemyCell ? (new SolidBrush(Color.Red)) : new SolidBrush(Color.White));
  }
  public void draw(Graphics g)
  {
    if (NorthWall) g.DrawLine(Pen, new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Right, Bounds.Top));
    if (SouthWall) g.DrawLine(Pen, new Point(Bounds.Left, Bounds.Bottom), new Point(Bounds.Right, Bounds.Bottom));
    if (WestWall) g.DrawLine(Pen, new Point(Bounds.Left, Bounds.Top), new Point(Bounds.Left, Bounds.Bottom));
    if (EastWall) g.DrawLine(Pen, new Point(Bounds.Right, Bounds.Top), new Point(Bounds.Right, Bounds.Bottom));

    var bounds = new Rectangle(this.Bounds.X + 1, this.Bounds.Y + 1, this.Bounds.Width - 1, this.Bounds.Height - 1);
    g.FillRectangle(this.color, bounds);
  }
  public void Visit(Graphics g)
  {
    var bounds = new Rectangle(this.Bounds.X + 3, this.Bounds.Y + 3, this.Bounds.Width - 6, this.Bounds.Height - 6);
    g.FillRectangle(new SolidBrush(Color.Black), bounds);
  }

  public void Clear(Graphics g)
  {
    var bounds = new Rectangle(this.Bounds.X + 3, this.Bounds.Y + 3, this.Bounds.Width - 6, this.Bounds.Height - 6);
    g.FillRectangle(new SolidBrush(Color.White), bounds);
  }
  public Cell(Point location, Size size, ref Dictionary<string, Cell> cellList, int r, int c, int maxR, int maxC)
  {
    this.Bounds = new Rectangle(location, size);
    this.Column = c;
    this.Row = r;
    this.id = "c" + c + "r" + r;
    int rowNort = r - 1;
    int rowSout = r + 1;
    int colEast = c + 1;
    int colWest = c - 1;
    NeighborNorthID = "c" + c + "r" + rowNort;
    NeighborSouthID = "c" + c + "r" + rowSout;
    NeighborEastID = "c" + colEast + "r" + r;
    NeighborWestID = "c" + colWest + "r" + r;
    if (rowNort < 0) NeighborNorthID = "none";
    if (rowSout > maxR) NeighborSouthID = "none";
    if (colEast > maxC) NeighborEastID = "none";
    if (colWest < 0) NeighborWestID = "none";
    this.Cells = cellList;
    this.Cells.Add(this.id, this);
  }
  public Cell getNeighbor()
  {
    List<Cell> c = new List<Cell>();
    if (!(NeighborNorthID == "none") && Cells[NeighborNorthID].Visited == false) c.Add(Cells[NeighborNorthID]);
    if (!(NeighborSouthID == "none") && Cells[NeighborSouthID].Visited == false) c.Add(Cells[NeighborSouthID]);
    if (!(NeighborEastID == "none") && Cells[NeighborEastID].Visited == false) c.Add(Cells[NeighborEastID]);
    if (!(NeighborWestID == "none") && Cells[NeighborWestID].Visited == false) c.Add(Cells[NeighborWestID]);
    int max = c.Count;
    Cell currentCell = null;
    if (c.Count > 0)
    {
      Microsoft.VisualBasic.VBMath.Randomize();
      int index = Convert.ToInt32(Conversion.Int(c.Count * VBMath.Rnd()));
      currentCell = c[index];
    }
    return currentCell;
  }
  public Cell Dig(ref Stack<Cell> stack)
  {
    this.Stack = stack;
    Cell nextCell = getNeighbor();
    if ((nextCell != null))
    {
      stack.Push(nextCell);
      if (nextCell.id == this.NeighborNorthID)
      {
        this.NorthWall = false;
        nextCell.SouthWall = false;
      }
      else if (nextCell.id == this.NeighborSouthID)
      {
        this.SouthWall = false;
        nextCell.NorthWall = false;
      }
      else if (nextCell.id == this.NeighborEastID)
      {
        this.EastWall = false;
        nextCell.WestWall = false;
      }
      else if (nextCell.id == this.NeighborWestID)
      {
        this.WestWall = false;
        nextCell.EastWall = false;
      }
    }
    else if (!(stack.Count == 0))
    {
      nextCell = stack.Pop();
    }
    else
    {
      return null;
    }
    return nextCell;
  }

  public override List<QState> GetAvailableStates()
  {
    var result = new List<QState>();

    if (!NorthWall) result.Add(Cells[NeighborNorthID]);
    if (!SouthWall) result.Add(Cells[NeighborSouthID]);
    if (!WestWall) result.Add(Cells[NeighborWestID]);
    if (!EastWall) result.Add(Cells[NeighborEastID]);

    return result;
  }

  public override QState GetRandNextState(QState prevState)
  {
    var rng = new Random();
    var rand = rng.NextDouble();
    Cell result = null;
    var possibilites = this.GetAvailableStates();

    while (result == null)
    {
      if (0 <= rand && rand < 0.25 && !NorthWall)
        result = Cells[NeighborNorthID];
      if (0.25 <= rand && rand < 0.5 && !SouthWall)
        result = Cells[NeighborSouthID];
      if (0.5 <= rand && rand < 0.75 && !WestWall)
        result = Cells[NeighborWestID];
      if (0.75 <= rand && rand <= 1 && !EastWall)
        result = Cells[NeighborEastID];

      if (prevState != null && result != null && result.Equals((Cell)prevState) && rng.NextDouble() >= 0.6)
      {
        result = null;
      }

      rand = rng.NextDouble();
    }
    return result;
  }

  private bool usedReward = false;

  public override void SetReward()
  {
    this.usedReward = false;
  }

  public override IQReward GetReward()
  {
    if (this.Row == Cells.Last().Value.Row && this.Column == Cells.Last().Value.Column)
    {
      return new CellReward(300);
    };

    if (this.state == State.EnemyCell || this.state == State.BonusCell)
    {
      if (this.usedReward)
        return new CellReward(-0.05);
      this.usedReward = true;
    };

    return new CellReward((double)(this.state == State.EnemyCell ? -10 : state == State.BonusCell ? 30 : -0.05));
  }

  public override bool Equals(object obj)
  {
    return this.Row == (obj as Cell).Row && this.Column == (obj as Cell).Column;
  }

  public override string ToString()
  {
    return $" (ROW:{this.Row};COL:{this.Column}) ";
  }
}

public class CellReward : IQReward
{
  double value;
  public CellReward(double _value)
  {
    value = _value;
  }
  public double ComputeReward()
  {
    return (double)value;
  }
}