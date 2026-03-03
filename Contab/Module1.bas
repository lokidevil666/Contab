Attribute VB_Name = "Module1"
Option Explicit

Global DataBase, SQL As String

Global Const MAXINT = 32767

Global Const color_grid = &HFFFFFF
Global Const colorA = &HB5B5FF    '&H565AFE
Global Const colorB = &HFFCAB3    '&HF8A867
Global Const colorC = &HB1E8AE    '&H92EC8C
Global Const colorD = &HC6FEFF    '&H9BF5FD
Global Const colorE = &H8CDAFF    '&H17A7FD
Global Const colorF = &HFFFFC0
Global Const colorG = &HFFC0FF
Global Const colorH = &HFF00

'Global Const colorF = &HE0E0E0
Global Const colorX = &HFFFFFF

Global programVersion, Programpath, sConnectionString, sConnectionERPString, sConnectionTSTString, Erro, ADMINtipo, SQLContas, FlXTmP As String
Global StatDESC, SQLServer, SQLDBase, SQLUser, SQLPass, SQLcmd, SAPUser, SAPPass, Conf, ConfSTR, ConfIN, ConfOPT, Arg1, Arg2, Arg3, Arg4 As String
Global FileName, FlSQLName, SQL1, CFile, T7, FlX(100), LG, TXTSaida, PathSaida, PathEntrada, Cb9Opt, SepDec, WhrContab, T7Sp, HdrX3 As String
Global HDRcab, HDRtmp, HDRtipoDOC, X3ANLeixo1, X3ANLeixo2, X3ANLeixo3, X3ANLeixo4, X3ANLeixo5, X3ANLeixo6, X3ANLeixo7, X3ANLeixo8 As String
Global X3ANLcnt1, X3ANLcnt2, X3ANLcnt3, X3ANLcnt4, X3ANLcnt5, X3ANLcnt6, X3ANLcnt7, X3ANLcnt8, X3Estab, X3ExConta, Agregado, HoldingFlag As String
Global Lingua, LGGrid(120), LGmenu(100), LGerroA(100), LGBotao(100), LGLabel(150), LGErro(100), LGBox(200), LGEstado(50), LGStatus(50), LGCase(20), LGTxtBox(100) As String
Global SQLExecaoPassagem(40), Estrutura(10), SQLSageNavCom, Combo6AccCode, AgregadoVALOR, AgregadoTXT, Sociedade, SocView, CntDta As String
Global DescERP As String
Global StatID, OP, NumOP, RowSel, XInicial, YInicial, Comissao, m_Row, m_Col, HDRCnt, RX3n1, RX3n2, TblLabelBackIN, TblLabelBackOFF As Integer
Global LayOutOpt, Grid22Ra, Grid22Ca, Grid22Rb, Grid22Cb, RepAsterisco, AgregadoRow, TipoACESSO, RegrasMemory, AGRtxtID, RgrSuso  As Integer
Global AgregadoMNT, CntMntPos, CntMntNeg, CntNmrDia, CntReg As Double
Global Ck7 As Integer
Global DLLop, Grid8Chg, AGReset, AutoMode, SXAdv, TXTRFCexport As Boolean
Global MenuCorIN, MenuCorIN2, MenuCorOFF, MenuFontIN, MenuFontOFF, TblLabelNormal, TblLabelNrmOpt, TblLabelEspecial, TblLabelEsp2, AgrSN, TBCmp, CmprcaoDia As String
Global Dbg, Dbug, DBOpen, DBOpenERP, DBOpenTST, StrDB, AnoOP, MesOP, UpCOM, OpLG, Estr_Act, TblLabelBoldIN, TblLabelBoldOFF, DHWrite As Boolean
Global conn As ADODB.Connection
Global connERP As ADODB.Connection
Global connTST As ADODB.Connection


