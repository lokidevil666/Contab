VERSION 5.00
Begin VB.Form FrmIntro 
   Appearance      =   0  'Flat
   BackColor       =   &H80000005&
   BorderStyle     =   0  'None
   ClientHeight    =   1455
   ClientLeft      =   0
   ClientTop       =   0
   ClientWidth     =   7515
   ForeColor       =   &H00808000&
   Icon            =   "FrmIntro.frx":0000
   Picture         =   "FrmIntro.frx":000C
   ScaleHeight     =   1455
   ScaleWidth      =   7515
   ShowInTaskbar   =   0   'False
   StartUpPosition =   2  'CenterScreen
   Begin VB.Timer Timer1 
      Interval        =   3000
      Left            =   0
      Top             =   720
   End
   Begin VB.Label Label2 
      BackStyle       =   0  'Transparent
      BeginProperty Font 
         Name            =   "MS Sans Serif"
         Size            =   8.25
         Charset         =   0
         Weight          =   700
         Underline       =   0   'False
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      ForeColor       =   &H00808000&
      Height          =   255
      Left            =   120
      TabIndex        =   0
      Top             =   1200
      Width           =   5295
   End
End
Attribute VB_Name = "FrmIntro"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False


Private Sub Form_Load()
Dim w, h As Integer

w = (Screen.Width \ 2) - (FrmIntro.Width \ 2)
h = (Screen.Height \ 2) - (FrmIntro.Height \ 2)
FrmIntro.Left = w
FrmIntro.Top = h
    
End Sub


Private Sub Timer1_Timer()

frmProses.Show
Unload FrmIntro
Timer1.Enabled = False


End Sub
