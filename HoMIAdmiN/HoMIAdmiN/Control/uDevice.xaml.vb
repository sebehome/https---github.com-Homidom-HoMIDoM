﻿Imports HoMIDom.HoMIDom
Imports HoMIDom.HoMIDom.Device
Imports HoMIDom.HoMIDom.Api
Imports System.IO
Imports System.Threading
Imports System.Text.RegularExpressions
Imports System.ComponentModel


Partial Public Class uDevice

    '--- Variables ------------------
    Public Event CloseMe(ByVal MyObject As Object, ByVal Cancel As Boolean)
    Public Event RefreshMe()
    Dim _Action As EAction 'Définit si modif ou création d'un device
    Dim _DeviceId As String = "" 'Id du device à modifier
    Dim FlagNewCmd, FlagNewDevice As Boolean
    Dim _Driver As HoMIDom.HoMIDom.TemplateDriver = Nothing
    Dim x As HoMIDom.HoMIDom.TemplateDevice = Nothing
    Dim ListeDrivers As List(Of TemplateDriver)
    Dim flagnewdev As Boolean = False
    Dim _ListVar As New Dictionary(Of String, String)

    Public Sub New(ByVal Action As Classe.EAction, ByVal DeviceId As String)
        Try
            ' Cet appel est requis par le Concepteur Windows Form.
            InitializeComponent()

            ' Ajoutez une initialisation quelconque après l'appel InitializeComponent().
            _DeviceId = DeviceId
            _Action = Action

            'Liste les type de devices dans le combo
            For Each value As ListeDevices In [Enum].GetValues(GetType(HoMIDom.HoMIDom.Device.ListeDevices))
                CbType.Items.Add(value.ToString)
            Next

            'Liste les drivers
            ListeDrivers = myService.GetAllDrivers(IdSrv)

            'Nouveau Device
            If Action = EAction.Nouveau Then

                'ajout de tous les drivers actif au combo
                For i As Integer = 0 To ListeDrivers.Count - 1
                    If ListeDrivers.Item(i).Enable Then CbDriver.Items.Add(ListeDrivers.Item(i).Nom)
                Next

                FlagNewDevice = True
                ImgDevice.Tag = ""
                CbType.IsEnabled = False

                'on cache des champs qui ne seront rendu visible que si le type selectionné le necessite
                StkValueMINMAX.Visibility = Windows.Visibility.Collapsed
                StkValueDefaultPrecision.Visibility = Windows.Visibility.Collapsed
                StkValueCorrectionFormatage.Visibility = Windows.Visibility.Collapsed
                StkRGBW.Visibility = Windows.Visibility.Collapsed
                ChKSolo.Visibility = Windows.Visibility.Collapsed
                BtnLearn1.Visibility = Windows.Visibility.Collapsed
                BtnLearn2.Visibility = Windows.Visibility.Collapsed

                'si c'est un nouveau device créé depuis un autocreateddevice alors on pré-rempli certains champs
                If NewDevice IsNot Nothing Then
                    TxtNom.Text = NewDevice.Name
                    TxtAdresse1.Text = NewDevice.Adresse1
                    TxtAdresse2.Text = NewDevice.Adresse2
                    CbDriver.SelectedValue = myService.ReturnDriverByID(IdSrv, NewDevice.IdDriver).Nom
                    CbType.IsEnabled = True
                    CbType.SelectedValue = NewDevice.Type
                    CbType_MouseLeave(CbType, Nothing)
                    flagnewdev = True
                End If

            Else 'Modification d'un Device

                FlagNewDevice = False
                x = myService.ReturnDeviceByID(IdSrv, DeviceId)

                If x IsNot Nothing Then 'on a trouvé le device

                    _Driver = myService.ReturnDriverByID(IdSrv, x.DriverID)
                    'ajout des drivers compatibles avec ce type de device au combo
                    For i As Integer = 0 To ListeDrivers.Count - 1
                        'pour chaque driver on regarde si le type est compatible
                        If ListeDrivers.Item(i).DeviceSupport.Count > 0 Then
                            For j As Integer = 0 To ListeDrivers.Item(i).DeviceSupport.Count - 1
                                If ListeDrivers.Item(i).DeviceSupport.Item(j).ToString = x.Type.ToString Then
                                    'on ajoute le drier a la liste si il est enable ou si il correspond à notre device
                                    If ListeDrivers.Item(i).Enable Or ListeDrivers.Item(i).Nom = _Driver.Nom Then CbDriver.Items.Add(ListeDrivers.Item(i).Nom)
                                    Exit For
                                End If
                            Next
                        End If
                    Next

                    'Affiche les propriétés du device
                    TxtNom.Text = x.Name
                    ChkEnable.IsChecked = x.Enable
                    ChkHisto.IsChecked = x.IsHisto
                    ChkHisto.ToolTip = "Le composant doit-il etre historisé, cocher pour historiser"
                    ChKSolo.IsChecked = x.Solo
                    ChKLastEtat.IsChecked = x.LastEtat
                    ChKAllValue.IsChecked = x.AllValue
                    TxtUnit.Text = x.Unit
                    TxtRefreshHisto.Text = x.RefreshHisto
                    TxtRefreshHisto.ToolTip = "Nombres de refresh avant d'enregistrer le composant dans l'historique, tout est historisé si 0 "
                    TxtPurge.Text = x.Purge
                    TxtPurge.ToolTip = "Nombres de jours avant de supprimer le composant de l'historique, pas de suppression si 0"
                    TxtMoyHeure.Text = x.MoyHeure
                    TxtMoyHeure.ToolTip = "Nombres de jours avant de faire une moyenne heure du composant de l'historique, pas de moyenne si 0"
                    TxtMoyJour.Text = x.MoyJour
                    TxtMoyJour.ToolTip = "Nombres de jours avant de faire une moyenne jour du composant de l'historique, pas de moyenne si 0"
                    TxtPuissance.Text = x.Puissance
                    TxtDescript.Text = x.Description
                    If TxtPuissance.Text = "" Then TxtPuissance.Text = "0"
                    If _Driver IsNot Nothing Then
                        CbDriver.SelectedValue = _Driver.Nom
                    End If
                    CbType.SelectedValue = x.Type.ToString
                    CbType.IsEnabled = False
                    CbType.Foreground = System.Windows.Media.Brushes.Black
                    TxtAdresse1.Text = x.Adresse1
                    TxtAdresse2.Text = x.Adresse2
                    CBModele.Text = x.Modele
                    TxtModele.Text = x.Modele
                    TxtRefresh.Text = x.Refresh
                    TxtLastChangeDuree.Text = x.LastChangeDuree
                    TxtID.Text = x.ID
                    _ListVar = x.VariablesOfDevice

                    Refresh_cbVar()

                    'affichage de l'image du composant
                    ImgDevice.Source = ConvertArrayToImage(myService.GetByteFromImage(x.Picture))
                    ImgDevice.Tag = x.Picture

                    'gestion des champs des composants avec VALUE INTEGER/DOUBLE/LONG
                    If AsProperty(x, "ValueMin") And AsProperty(x, "ValueMax") Then
                        StkValueMINMAX.Visibility = Windows.Visibility.Visible
                        TxtValueMin.Text = x.ValueMin
                        TxtValueMax.Text = x.ValueMax
                    Else : StkValueMINMAX.Visibility = Windows.Visibility.Collapsed
                    End If
                    If AsProperty(x, "ValueDef") And AsProperty(x, "precision") Then
                        StkValueDefaultPrecision.Visibility = Windows.Visibility.Visible
                        TxtValDef.Text = x.ValueDef
                        TxtPrecision.Text = x.Precision
                    Else : StkValueDefaultPrecision.Visibility = Windows.Visibility.Collapsed
                    End If
                    If AsProperty(x, "Correction") And AsProperty(x, "Formatage") Then
                        StkValueCorrectionFormatage.Visibility = Windows.Visibility.Visible
                        TxtCorrection.Text = x.Correction
                        TxtFormatage.Text = x.Formatage
                    Else : StkValueCorrectionFormatage.Visibility = Windows.Visibility.Collapsed
                    End If

                    'gestion des champs des composants LAMPERGBW
                    If x.Type = ListeDevices.LAMPERGBW Then
                        StkRGBW.Visibility = Windows.Visibility.Visible
                        TxtRGBWred.Text = x.red
                        TxtRGBWgreen.Text = x.green
                        TxtRGBWblue.Text = x.blue
                        TxtRGBWwhite.Text = x.white
                        TxtRGBWtemperature.Text = x.temperature
                        TxtRGBWspeed.Text = x.speed
                        TxtRGBWoptionnal.Text = x.optionnal
                    Else : StkRGBW.Visibility = Windows.Visibility.Collapsed
                    End If


                    BtnTest.Visibility = Windows.Visibility.Visible
                    StkModel.Visibility = Visibility.Visible
                    If x.Type = ListeDevices.MULTIMEDIA Then
                        ImgEditTemplate.Visibility = Windows.Visibility.Visible
                        LabelModele.Visibility = Windows.Visibility.Visible
                        LabelModele.Content = "Template"
                        TxtModele.Visibility = Windows.Visibility.Collapsed
                        CBModele.Items.Clear()
                        CBModele.ItemsSource = myService.GetListOfTemplate
                        CBModele.DisplayMemberPath = "Name"
                        CBModele.Visibility = Windows.Visibility.Visible
                        Dim idx As Integer = 0
                        For Each itm In CBModele.Items
                            If itm.id = x.Modele Then
                                CBModele.SelectedIndex = idx
                                Exit For
                            End If
                            idx += 1
                        Next
                        StkDriver.Visibility = Windows.Visibility.Collapsed
                        LabelModele.ToolTip = "Template à utiliser"
                        CBModele.ToolTip = "Template à utiliser"
                        StkType.Margin = New Thickness(0)
                    End If

                    'on verifie si le device est un device systeme pour le rendre NON modifiable
                    If Left(x.Name, 5) = "HOMI_" Then
                        Label1.Content = "Composant SYSTEME"
                        TxtNom.IsReadOnly = True
                        TxtDescript.IsReadOnly = True
                        CbDriver.IsEditable = False
                        CbDriver.IsReadOnly = True
                        CbDriver.IsEnabled = False
                        CbDriver.Foreground = System.Windows.Media.Brushes.Black
                        ChkEnable.Visibility = Windows.Visibility.Collapsed
                        ChKSolo.Visibility = Windows.Visibility.Collapsed
                        StkAdr1.Visibility = Windows.Visibility.Collapsed
                        StkAdr2.Visibility = Windows.Visibility.Collapsed
                        CBModele.Visibility = Windows.Visibility.Collapsed
                        StkLastChange.Visibility = Windows.Visibility.Collapsed
                        StkRefresh.Visibility = Windows.Visibility.Collapsed
                        BtnTest.Visibility = Windows.Visibility.Collapsed
                        LabelModele.Visibility = Windows.Visibility.Collapsed
                        StkUnit.Visibility = Windows.Visibility.Collapsed
                        StkPuiss.Visibility = Windows.Visibility.Collapsed
                    End If

                    'Affiche du bouton Historique, avec un tooltip si il y a des valeurs
                    BtnHisto.Visibility = Windows.Visibility.Visible
                    Dim tl As New ToolTip
                    tl.Foreground = System.Windows.Media.Brushes.White
                    tl.Background = System.Windows.Media.Brushes.WhiteSmoke
                    tl.BorderBrush = System.Windows.Media.Brushes.Black
                    Dim stkpopup As New StackPanel
                    Dim tool As New Label
                    Dim nbhisto As Double = myService.DeviceAsHisto(DeviceId)
                    If nbhisto > 0 Then
                        tool.Content &= "Derniere Valeur: " & x.Value & vbCrLf
                        tool.Content &= "Date MAJ: " & x.LastChange & vbCrLf
                        tool.Content &= "Nb Histo: " & nbhisto & vbCrLf
                    Else
                        BtnHisto.FontStyle = System.Windows.FontStyles.Italic
                        tool.Content = "Aucun Historique"
                    End If
                    stkpopup.Children.Add(tool)
                    tool = Nothing
                    tl.Content = stkpopup
                    stkpopup = Nothing
                    BtnHisto.ToolTip = tl
                End If
            End If

            'Liste toutes les zones dans la liste
            Dim _listezone = myService.GetAllZones(IdSrv)
            For i As Integer = 0 To _listezone.Count - 1
                Dim ch1 As New CheckBox
                Dim ch2 As New CheckBox
                Dim ImgZone As New Image
                Dim stk As New StackPanel
                stk.Orientation = Orientation.Horizontal
                ImgZone.Width = 32
                ImgZone.Height = 32
                ImgZone.Margin = New Thickness(2)
                ImgZone.Source = ConvertArrayToImage(myService.GetByteFromImage(_listezone.Item(i).Icon))
                ch1.Width = 80
                ch1.Content = _listezone.Item(i).Name
                ch1.ToolTip = ch1.Content
                ch1.Uid = _listezone.Item(i).ID
                AddHandler ch1.Click, AddressOf ChkElement_Click
                ch2.Content = "Visible"
                ch2.ToolTip = "Visible dans la zone côté client"
                ch2.Visibility = Windows.Visibility.Collapsed
                For j As Integer = 0 To _listezone.Item(i).ListElement.Count - 1
                    If _listezone.Item(i).ListElement.Item(j).ElementID = _DeviceId Then
                        ch1.IsChecked = True
                        ch2.Visibility = Windows.Visibility.Visible
                        If _listezone.Item(i).ListElement.Item(j).Visible = True Then ch2.IsChecked = True
                        Exit For
                    End If

                Next
                stk.Children.Add(ImgZone)
                stk.Children.Add(ch1)
                stk.Children.Add(ch2)
                ListZone.Items.Add(stk)

            Next
        Catch Ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur: " & Ex.ToString, "Erreur", "New")
        End Try
    End Sub

    Private Sub SaveInZone()
        Try
            For i As Integer = 0 To ListZone.Items.Count - 1
                Dim stk As StackPanel = ListZone.Items(i)
                Dim x1 As CheckBox = stk.Children.Item(1)
                Dim x2 As CheckBox = stk.Children.Item(2)
                Dim trv As Boolean = False

                For Each dev In myService.GetDeviceInZone(IdSrv, x1.Uid)
                    If dev IsNot Nothing Then
                        If dev = _DeviceId Then
                            trv = True
                            Exit For
                        End If
                    End If

                Next

                If trv = True And x1.IsChecked = False Then
                    myService.DeleteDeviceToZone(IdSrv, x1.Uid, _DeviceId)
                Else
                    If trv = True And x1.IsChecked = True Then
                        myService.AddDeviceToZone(IdSrv, x1.Uid, _DeviceId, x2.IsChecked)
                    Else
                        If trv = False And x1.IsChecked = True Then myService.AddDeviceToZone(IdSrv, x1.Uid, _DeviceId, x2.IsChecked)
                    End If
                End If

            Next
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur dans le programme SaveInZone: " & ex.ToString, "Admin", "SaveInZone")
        End Try

    End Sub

    'Modif des champs suivant le driver sélectionné
    Private Sub CbDriver_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles CbDriver.SelectionChanged
        MaJDriver()
    End Sub

    Private Sub MaJDriver()
        Try
            Dim _Driver As Object = Nothing
            Me.ForceCursor = True
            'on cherche le driver
            If CbDriver.SelectedItem IsNot Nothing Then
                For i As Integer = 0 To ListeDrivers.Count - 1
                    If ListeDrivers.Item(i).Nom = CbDriver.SelectedItem.ToString Then
                        _Driver = myService.ReturnDriverByID(IdSrv, ListeDrivers.Item(i).ID)
                        Exit For
                    End If
                Next
            End If

            'si on a trouvé le driver selectionné
            If _Driver IsNot Nothing Then
                'Si c'est un nouveau device, on peut modifier le type sinon non
                If FlagNewDevice Then
                    Dim mem As String = CbType.Text
                    CbType.IsEnabled = True
                    CbType.Items.Clear()

                    For j As Integer = 0 To _Driver.DeviceSupport.count - 1
                        CbType.Items.Add(_Driver.DeviceSupport.item(j).ToString)
                    Next
                    CbType.IsEnabled = True
                    CbType.Text = mem
                End If

                If _Driver.ID = "8B8EFC7E-F66D-11E1-AD40-71AF6188709B" Then 'And CbAdresse1.Tag <> "WEATHERMETEO" 
                    CbAdresse1.Tag = "WEATHERMETEO"
                    CbAdresse1.Items.Clear()

                    Dim CityID As New Dictionary(Of String, String)
                    CityID = GetWeatherCityID()

                    CbAdresse1.ItemsSource = New Forms.BindingSource(CityID, Nothing)
                    CbAdresse1.DisplayMemberPath = "Value"
                    CbAdresse1.SelectedValuePath = "Key"

                    'TxtAdresse1.Visibility = Windows.Visibility.Collapsed
                    CbAdresse1.Visibility = Windows.Visibility.Visible
                Else
                    CbAdresse1.Tag = "Driver"
                    CbAdresse1.Text = ""
                    CbAdresse1.ItemsSource = Nothing
                    CbAdresse1.Items.Clear()
                    TxtAdresse1.Visibility = Windows.Visibility.Visible
                    CbAdresse1.Visibility = Windows.Visibility.Collapsed
                    CbAdresse2.Visibility = Windows.Visibility.Collapsed
                End If

                'Suivant le driver, on change les champs (personnalisation des Labels, affichage suivant @...
                If _Driver.LabelsDevice.Count > 0 Then
                    For k As Integer = 0 To _Driver.LabelsDevice.Count - 1
                        Select Case UCase(_Driver.LabelsDevice.Item(k).NomChamp)
                            Case "ADRESSE1"
                                If _Driver.LabelsDevice.Item(k).LabelChamp = "@" Then
                                    CbAdresse1.Visibility = Windows.Visibility.Collapsed
                                    CbAdresse1.Tag = "Driver"
                                    TxtAdresse1.Visibility = Windows.Visibility.Collapsed
                                    TxtAdresse1.Tag = 0
                                    LabelAdresse1.Visibility = Windows.Visibility.Collapsed
                                    StkModel.Visibility = Windows.Visibility.Collapsed
                                Else
                                    LabelAdresse1.Visibility = Windows.Visibility.Visible
                                    StkModel.Visibility = Windows.Visibility.Visible
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).LabelChamp) = False Then LabelAdresse1.Content = _Driver.LabelsDevice.Item(k).LabelChamp
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Tooltip) = False Then
                                        LabelAdresse1.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                        CbAdresse1.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                    End If
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Parametre) = False Then
                                        CbAdresse1.Items.Clear()
                                        Dim a() As String = _Driver.LabelsDevice.Item(k).Parametre.Split("|")
                                        If a.Count > 1 Then
                                            For g As Integer = 0 To a.Length - 1
                                                CbAdresse1.Items.Add(a(g))
                                            Next
                                            CbAdresse1.IsEditable = False
                                            CbAdresse1.Visibility = Windows.Visibility.Visible
                                            CbAdresse1.Tag = "Driver"
                                            TxtAdresse1.Visibility = Windows.Visibility.Collapsed
                                            TxtAdresse1.Tag = 0
                                        Else
                                            CbAdresse1.Visibility = Windows.Visibility.Collapsed
                                            CbAdresse1.Tag = "Driver"
                                            TxtAdresse1.Visibility = Windows.Visibility.Visible
                                            TxtAdresse1.Tag = 1
                                            TxtAdresse1.Text = a(0)
                                        End If
                                    Else
                                        CbAdresse1.Visibility = Windows.Visibility.Collapsed
                                        CbAdresse1.Tag = "Driver"
                                        TxtAdresse1.Visibility = Windows.Visibility.Visible
                                        TxtAdresse1.Tag = 1
                                    End If
                                End If
                            Case "ADRESSE2"
                                If _Driver.LabelsDevice.Item(k).LabelChamp = "@" Then
                                    StkAdr2.Visibility = Windows.Visibility.Collapsed
                                    CbAdresse2.Visibility = Windows.Visibility.Collapsed
                                    CbAdresse2.Tag = "Driver"
                                Else
                                    LabelAdresse2.Content = _Driver.LabelsDevice.Item(k).LabelChamp
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Tooltip) = False Then
                                        LabelAdresse2.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                        TxtAdresse2.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                        CbAdresse2.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                    End If
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Parametre) = False Then
                                        CbAdresse2.Items.Clear()
                                        Dim a() As String = _Driver.LabelsDevice.Item(k).Parametre.Split("|")
                                        If a.Count > 1 Then
                                            For g As Integer = 0 To a.Length - 1
                                                If Not InStr(a(g), "#;") > 0 Then  'permet de ne prendre que les valeurs à ne pas lier à adresse1
                                                    CbAdresse2.Items.Add(a(g))
                                                End If

                                            Next
                                            CbAdresse2.IsEditable = False
                                            CbAdresse2.Visibility = Windows.Visibility.Visible
                                            CbAdresse2.Tag = "Driver"
                                            TxtAdresse2.Visibility = Windows.Visibility.Collapsed
                                            TxtAdresse2.Tag = 0
                                        Else
                                            CbAdresse2.Visibility = Windows.Visibility.Collapsed
                                            CbAdresse2.Tag = "Driver"
                                            TxtAdresse2.Visibility = Windows.Visibility.Visible
                                            TxtAdresse2.Tag = 1
                                            TxtAdresse2.Text = a(0)
                                        End If
                                    Else
                                        CbAdresse2.Visibility = Windows.Visibility.Collapsed
                                        CbAdresse2.Tag = "Driver"
                                        TxtAdresse2.Visibility = Windows.Visibility.Visible
                                        TxtAdresse2.Tag = 1
                                    End If


                                    StkAdr2.Visibility = Windows.Visibility.Visible
                                End If
                            Case "SOLO"
                                If _Driver.LabelsDevice.Item(k).LabelChamp = "@" Then
                                    ChKSolo.Visibility = Windows.Visibility.Collapsed
                                Else
                                    ChKSolo.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                    ChKSolo.Visibility = Windows.Visibility.Visible
                                End If
                            Case "REFRESH"
                                If _Driver.LabelsDevice.Item(k).LabelChamp = "@" Then
                                    StkRefresh.Visibility = Windows.Visibility.Collapsed
                                Else
                                    LabelRefresh.Content = _Driver.LabelsDevice.Item(k).LabelChamp
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Tooltip) = False Then
                                        LabelRefresh.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                        TxtRefresh.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                    End If
                                    StkRefresh.Visibility = Windows.Visibility.Visible
                                End If
                            Case "LASTCHANGEDUREE"
                                If _Driver.LabelsDevice.Item(k).LabelChamp = "@" Then
                                    StkLastChange.Visibility = Windows.Visibility.Collapsed
                                Else
                                    LabelLastChangeDuree.Content = _Driver.LabelsDevice.Item(k).LabelChamp
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Tooltip) = True Then
                                        TxtLastChangeDuree.ToolTip = "Permet de vérifier si le composant a été mis à jour depuis moins de x minutes sinon il apparait en erreur"
                                        LabelLastChangeDuree.ToolTip = "Permet de vérifier si le composant a été mis à jour depuis moins de x minutes sinon il apparait en erreur"
                                    Else
                                        TxtLastChangeDuree.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                        LabelLastChangeDuree.ToolTip = _Driver.LabelsDevice.Item(k).Tooltip
                                    End If
                                    StkLastChange.Visibility = Windows.Visibility.Visible
                                End If
                            Case "MODELE"
                                If _Driver.LabelsDevice.Item(k).LabelChamp = "@" Then
                                    CBModele.Visibility = Windows.Visibility.Collapsed
                                    CBModele.Tag = 0
                                    TxtModele.Visibility = Windows.Visibility.Collapsed
                                    TxtModele.Tag = 0
                                    LabelModele.Visibility = Windows.Visibility.Collapsed
                                    StkModel.Visibility = Windows.Visibility.Collapsed
                                Else
                                    LabelModele.Visibility = Windows.Visibility.Visible
                                    StkModel.Visibility = Windows.Visibility.Visible
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).LabelChamp) = False Then LabelModele.Content = _Driver.LabelsDevice.Item(k).LabelChamp
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Tooltip) = False Then
                                        LabelModele.ToolTip = _Driver.LabelsDevice.Item(k).ToolTip
                                        CBModele.ToolTip = _Driver.LabelsDevice.Item(k).ToolTip
                                    End If
                                    If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Parametre) = False Then
                                        CBModele.Items.Clear()
                                        Dim a() As String = _Driver.LabelsDevice.Item(k).Parametre.Split("|")
                                        If a.Length > 0 Then
                                            For g As Integer = 0 To a.Length - 1
                                                CBModele.Items.Add(a(g))
                                            Next
                                            CBModele.IsEditable = False
                                            CBModele.Visibility = Windows.Visibility.Visible
                                            CBModele.Tag = 1
                                            TxtModele.Visibility = Windows.Visibility.Collapsed
                                            TxtModele.Tag = 0
                                        Else
                                            CBModele.Visibility = Windows.Visibility.Collapsed
                                            CBModele.Tag = 0
                                            TxtModele.Visibility = Windows.Visibility.Visible
                                            TxtModele.Tag = 1
                                            TxtModele.Text = a(0)
                                        End If
                                    Else
                                        CBModele.Visibility = Windows.Visibility.Collapsed
                                        CBModele.Tag = 0
                                        TxtModele.Visibility = Windows.Visibility.Visible
                                        TxtModele.Tag = 1
                                    End If
                                End If
                        End Select
                    Next
                End If

                IsIR()

            End If
            Me.Cursor = Nothing
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur: " & ex.ToString, "Erreur", "MaJDrive")
        End Try
    End Sub

    Private Sub IsIR()
        Try
            If IsNothing(_Driver) Then Exit Sub
            If (IsNothing(BtnLearn1)) Or (IsNothing(BtnLearn2)) Then Exit Sub

            If CbType.Text <> "MULTIMEDIA" And _Driver.ID = "74FD4E7C-34ED-11E0-8AC4-70CEDED72085" Then
                BtnLearn1.Visibility = Windows.Visibility.Visible
                BtnLearn2.Visibility = Windows.Visibility.Visible
            Else
                BtnLearn1.Visibility = Windows.Visibility.Collapsed
                BtnLearn2.Visibility = Windows.Visibility.Collapsed
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR VerifDriver: " & ex.ToString, "ERREUR", "IsIR")
        End Try
    End Sub

    'Verification du driver suivant son ID
    Private Sub VerifDriver(ByVal IdDriver As String)
        Try
            Dim x As TemplateDriver = myService.ReturnDriverByID(IdSrv, IdDriver)
            If x IsNot Nothing Then
                If x.Enable = False Then
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.INFO, "Le driver " & x.Nom & " n'est pas activé (Enable), le composant ne pourra pas être utilisé!", "INFO", "")
                    Exit Sub
                End If
                If x.IsConnect = False Then AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.INFO, "Le driver " & x.Nom & " n'est pas démarré, le composant ne pourra pas être utilisé!", "INFO", "")
            Else
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le driver n'a pas pu être trouvé ! (ID du driver: " & IdDriver & ")", "ERREUR", "")
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR VerifDriver: " & ex.ToString, "ERREUR", "VerifDriver")
        End Try
    End Sub

    'Quand on change le TYPE d'un composant
    Private Sub CbType_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles CbType.MouseLeave
        Try
            If _Action = EAction.Nouveau Then
                'Gestion si Device avec Value
                If CbType.SelectedValue Is Nothing Then Exit Sub
                If CbType.Text = "BAROMETRE" _
                    Or CbType.Text = "COMPTEUR" _
                    Or CbType.Text = "ENERGIEINSTANTANEE" _
                    Or CbType.Text = "ENERGIETOTALE" _
                    Or CbType.Text = "GENERIQUEVALUE" _
                    Or CbType.Text = "HUMIDITE" _
                    Or CbType.Text = "LAMPE" _
                    Or CbType.Text = "LAMPERGBW" _
                    Or CbType.Text = "PLUIECOURANT" _
                    Or CbType.Text = "PLUIETOTAL" _
                    Or CbType.Text = "TEMPERATURE" _
                    Or CbType.Text = "TEMPERATURECONSIGNE" _
                    Or CbType.Text = "VITESSEVENT" _
                    Or CbType.Text = "UV" _
                    Or CbType.Text = "VOLET" _
                    Then
                    StkValueMINMAX.Visibility = Windows.Visibility.Visible
                    StkValueDefaultPrecision.Visibility = Windows.Visibility.Visible
                    StkValueCorrectionFormatage.Visibility = Windows.Visibility.Visible


                    'gestion des champs valuemin/max/defaut suivant le type
                    If CbType.Text = "HUMIDITE" Or CbType.Text = "LAMPE" Or CbType.Text = "LAMPERGBW" Or CbType.Text = "VOLET" Then
                        TxtValueMin.Text = "0"
                        TxtValueMax.Text = "100"
                        TxtValDef.Text = "0"
                        TxtCorrection.Text = "0"
                        TxtPrecision.Text = "0"
                    ElseIf CbType.Text = "BAROMETRE" _
                        Or CbType.Text = "COMPTEUR" _
                        Or CbType.Text = "ENERGIEINSTANTANEE" _
                        Or CbType.Text = "ENERGIETOTALE" _
                        Or CbType.Text = "GENERIQUEVALUE" _
                        Or CbType.Text = "PLUIECOURANT" _
                        Or CbType.Text = "PLUIETOTAL" _
                        Or CbType.Text = "VITESSEVENT" _
                        Or CbType.Text = "UV" Then
                        TxtValueMin.Text = "0"
                        TxtValueMax.Text = "9999999"
                        TxtValDef.Text = "0"
                        TxtCorrection.Text = "0"
                        TxtPrecision.Text = "0"
                    ElseIf CbType.Text = "TEMPERATURE" _
                        Or CbType.Text = "TEMPERATURECONSIGNE" _
                        Then
                        TxtValueMin.Text = "-99"
                        TxtValueMax.Text = "9999"
                        TxtValDef.Text = "0"
                        TxtCorrection.Text = "0"
                        TxtPrecision.Text = "0"
                    End If

                    If CbType.Text = "LAMPERGBW" Then
                        StkRGBW.Visibility = Windows.Visibility.Visible
                        TxtRGBWred.Text = 0
                        TxtRGBWgreen.Text = 0
                        TxtRGBWblue.Text = 0
                        TxtRGBWwhite.Text = 0
                        TxtRGBWtemperature.Text = 0
                        TxtRGBWspeed.Text = 0
                        TxtRGBWoptionnal.Text = ""
                    End If
                Else
                    StkValueMINMAX.Visibility = Windows.Visibility.Collapsed
                    StkValueDefaultPrecision.Visibility = Windows.Visibility.Collapsed
                    StkValueCorrectionFormatage.Visibility = Windows.Visibility.Collapsed
                    StkRGBW.Visibility = Windows.Visibility.Collapsed
                End If

                'Gestion AllValue et LastEtat par défaut
                If CbType.Text = "BATTERIE" Or CbType.Text = "FREEBOX" Or CbType.Text = "GENERIQUESTRING" Or CbType.Text = "MULTIMEDIA" Or CbType.Text = "SWITCH" Or CbType.Text = "TELECOMMANDE" Then
                    ChKLastEtat.IsChecked = False
                    ChKAllValue.IsChecked = True
                Else
                    ChKLastEtat.IsChecked = False
                    ChKAllValue.IsChecked = False
                End If

                StkModel.Visibility = Windows.Visibility.Visible
                If CbType.SelectedValue = "MULTIMEDIA" Then
                    StkDriver.Visibility = Windows.Visibility.Collapsed
                    ImgEditTemplate.Visibility = Windows.Visibility.Visible
                    LabelModele.Content = "Template"
                    LabelModele.ToolTip = "Template à utiliser"
                    CBModele.ToolTip = "Template à utiliser"
                    TxtModele.Visibility = Windows.Visibility.Collapsed
                    CBModele.ItemsSource = myService.GetListOfTemplate
                    CBModele.DisplayMemberPath = "Name"
                    CBModele.Visibility = Windows.Visibility.Visible
                    StkType.Margin = New Thickness(0)
                End If
            End If

            IsIR()
        Catch Ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur lors du changement de type: " & Ex.Message, "Erreur", "CbType_MouseLeave")
        End Try
    End Sub

    Private Sub TxtPuissance_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtPuissance.TextChanged
        Try
            If IsNumeric(TxtPuissance.Text) = False Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique !!", "ERREUR", "")
                TxtPuissance.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Sub uDevice TxtPuissance_TextChanged: " & ex.Message, "ERREUR", "TxtPuissance_TextChanged")
        End Try
    End Sub
    Private Sub TxtRefresh_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtRefresh.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtRefresh.Text)) Or (String.IsNullOrEmpty(TxtRefresh.Text) = False And IsNumeric(TxtRefresh.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtRefresh.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtRefresh_TextChanged: " & ex.ToString, "ERREUR", "TxtRefresh_TextChanged")
        End Try
    End Sub

    Private Sub TxtLastChangeDuree_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtLastChangeDuree.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtLastChangeDuree.Text)) Or (String.IsNullOrEmpty(TxtLastChangeDuree.Text) = False And IsNumeric(TxtLastChangeDuree.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtLastChangeDuree.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtLastChangeDuree_TextChanged: " & ex.ToString, "ERREUR", "TxtLastChangeDuree_TextChanged")
        End Try
    End Sub
    Private Sub TxtValueMin_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtValueMin.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtValueMin.Text)) Or (String.IsNullOrEmpty(TxtValueMin.Text) = False And IsNumeric(TxtValueMin.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtValueMin.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtValueMax_TextChanged: " & ex.ToString, "ERREUR", "TxtValueMin_TextChanged")
        End Try
    End Sub
    Private Sub TxtValueMax_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtValueMax.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtValueMax.Text)) Or (String.IsNullOrEmpty(TxtValueMax.Text) = False And IsNumeric(TxtValueMax.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtValueMax.Text = 9999999
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtValueMax_TextChanged: " & ex.ToString, "ERREUR", "TxtValueMax_TextChanged")
        End Try
    End Sub

    Private Sub TxtRefreshHisto_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtRefreshHisto.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtRefreshHisto.Text)) Or (String.IsNullOrEmpty(TxtRefreshHisto.Text) = False And IsNumeric(TxtRefreshHisto.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtRefreshHisto.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtRefreshHisto_TextChanged: " & ex.ToString, "ERREUR", "TxtRefreshHisto_TextChanged")
        End Try
    End Sub

    Private Sub TxtMoyHeure_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtMoyHeure.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtMoyHeure.Text)) Or (String.IsNullOrEmpty(TxtMoyHeure.Text) = False And IsNumeric(TxtMoyHeure.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtMoyHeure.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtMoyHeure_TextChanged: " & ex.ToString, "ERREUR", "TxtMoyHeure_TextChanged")
        End Try
    End Sub

    Private Sub TxtMoyJour_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtMoyJour.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtMoyJour.Text)) Or (String.IsNullOrEmpty(TxtMoyJour.Text) = False And IsNumeric(TxtMoyJour.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtMoyJour.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtMoyJour_TextChanged: " & ex.ToString, "ERREUR", "TxtMoyJour_TextChanged")
        End Try
    End Sub

    Private Sub TxtPurge_TextChanged(ByVal sender As System.Object, ByVal e As System.Windows.Controls.TextChangedEventArgs) Handles TxtPurge.TextChanged
        Try
            If (String.IsNullOrEmpty(TxtPurge.Text)) Or (String.IsNullOrEmpty(TxtPurge.Text) = False And IsNumeric(TxtPurge.Text) = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez saisir une valeur numérique")
                TxtPurge.Text = 0
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice TxtPurge_TextChanged: " & ex.ToString, "ERREUR", "TxtPurge_TextChanged")
        End Try
    End Sub


    Private Sub ImgDevice_MouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles ImgDevice.MouseLeftButtonDown
        Try
            Dim frm As New WindowImg
            frm.ShowDialog()
            If frm.DialogResult.HasValue And frm.DialogResult.Value Then
                Dim retour As String = frm.FileName
                If String.IsNullOrEmpty(retour) = False Then
                    ImgDevice.Source = ConvertArrayToImage(myService.GetByteFromImage(retour))
                    ImgDevice.Tag = retour
                End If
                frm.Close()
            Else
                frm.Close()
            End If
            frm = Nothing
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Sub ImgDevice_MouseLeftButtonDown: " & ex.Message, "ERREUR", "ImgDevice_MouseLeftButtonDown")
        End Try
    End Sub

    'Gestion des zones
    Private Sub ChkElement_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs)
        Try
            For Each stk As StackPanel In ListZone.Items
                Dim x As CheckBox = stk.Children.Item(1)
                If x.IsChecked = True Then
                    stk.Children.Item(2).Visibility = Windows.Visibility.Visible
                Else
                    stk.Children.Item(2).Visibility = Windows.Visibility.Collapsed
                End If
            Next
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice ChkElement_Click: " & ex.ToString, "ERREUR", "ChkElement_Click")
        End Try
    End Sub

    Private Sub UnloadControl(ByVal MyControl As Object)
        Try
            For i As Integer = 0 To Window1.CanvasUser.Children.Count - 1
                If Window1.CanvasUser.Children.Item(i).Uid = MyControl.uid Then
                    Window1.CanvasUser.Children.RemoveAt(i)
                    Exit Sub
                End If
            Next
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur uDevice UnloadControl: " & ex.ToString, "ERREUR", "UnloadControl")
        End Try
    End Sub

#Region "Gestion des BOUTONS"

    Private Sub BtnClose_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnClose.Click
        Try
            If (FlagChange) And (FlagNewDevice) Then
                If MessageBox.Show("Vous avez sauvegardé préalablement." & vbCrLf & "Cette action va annuler la sauvegarde." & vbCrLf & vbCrLf & "Voulez-vous continuer ?", "HomIAdmin", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                    If (Not String.IsNullOrEmpty(_DeviceId)) And (Not _Action = EAction.Modifier) Then
                        myService.DeleteDevice(IdSrv, _DeviceId)
                        flagnewdev = False
                        refreshtreeviewdevice = True
                    End If
                Else
                    Exit Sub
                End If
            End If

            FlagChange = False
            NewDevice = Nothing
            RaiseEvent CloseMe(Me, True)

        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Sub uDevice BtnClose: " & ex.ToString, "ERREUR", "BtnClose_Click")
        End Try
    End Sub

    Private Sub BtnOK_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnOK.Click
        'lance la sauvegarde et ferse la fenetre si OK
        Try
            'sauvegarde device
            If SaveDevice() Then
                RaiseEvent CloseMe(Me, False)
            End If

        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Sub uDevice BtnOK and Close: " & ex.ToString, "ERREUR", "BtnOK_Click")
        End Try
    End Sub

    Private Sub BtnSave_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnSave.Click
        ' lance la sauvegarde et reste sur la fenetre 
        Try
            refreshtreeviewdevice = TxtID.Text = ""  'si nouveau pas d'id
            'sauvegarde device
            If SaveDevice() Then
                BtnTest.Visibility = Windows.Visibility.Visible
                BtnHisto.Visibility = Windows.Visibility.Visible
                If CbType.SelectedValue = "MULTIMEDIA" Then
                    BtnEditTel.Visibility = Windows.Visibility.Visible
                    TxtModele.Visibility = Visibility.Hidden
                    LabelModele.Visibility = Windows.Visibility.Hidden
                End If
                If _DeviceId.Length > 3 Then x = myService.ReturnDeviceByID(IdSrv, _DeviceId)
            End If

        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Sub uDevice BtnSave_Click: " & ex.Message, "ERREUR", "BtnSave_Click")
        End Try
    End Sub

    Private Sub BtnTest_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnTest.Click
        Try
            If (myService.ReturnDeviceByID(IdSrv, _DeviceId) Is Nothing) Or (myService.ReturnDeviceByID(IdSrv, _DeviceId).Enable = False) Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Vous ne pouvez pas exécuter de commandes car le device n'est pas activé (propriété Enable)!", "Erreur", "")
                Exit Sub
            End If

            Dim y As New uTestDevice(_DeviceId)
            y.Uid = System.Guid.NewGuid.ToString()
            AddHandler y.CloseMe, AddressOf UnloadControl
            Window1.CanvasUser.Children.Add(y)
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur Tester: " & ex.Message, "Erreur", "BtnTest_Click")
        End Try
    End Sub


    Private Sub BtnHisto_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnHisto.Click
        Try
            If IsConnect = False Then
                Exit Sub
            End If

            Me.Cursor = Cursors.Wait

            If myService.DeviceAsHisto(_DeviceId, "Value") Then
                Dim Devices As New List(Of Dictionary(Of String, String))
                Dim y As New Dictionary(Of String, String)
                y.Add(_DeviceId, "Value")
                Devices.Add(y)

                Dim x As New uHisto(Devices)
                x.Uid = System.Guid.NewGuid.ToString()
                x.Width = Window1.CanvasUser.ActualWidth - 20
                x.Height = Window1.CanvasUser.ActualHeight - 20
                x._with = Window1.CanvasUser.ActualHeight - 20
                AddHandler x.CloseMe, AddressOf UnloadControl
                Window1.CanvasUser.Children.Add(x)

            End If
            Me.Cursor = Nothing
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur lors de la génération du relevé: " & ex.ToString, "Erreur Admin", "BtnHisto_Click")
        End Try
    End Sub

#End Region

    'gestion des options non compatibles entre elle
    Private Sub ChKAllValue_Checked(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles ChKAllValue.Click
        Try
            If ChKAllValue.IsChecked Then ChKLastEtat.IsChecked = False
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur ChKAllValue_Checked: " & ex.ToString, "Erreur Admin", "ChKAllValue_Checked")
        End Try
    End Sub
    Private Sub ChKLastEtat_Checked(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles ChKLastEtat.Click
        Try
            If ChKLastEtat.IsChecked Then ChKAllValue.IsChecked = False
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur ChKLastEtat_Checked: " & ex.ToString, "Erreur Admin", "ChKLastEtat_Checked")
        End Try
    End Sub

    Private Sub ImgEditTemplate_MouseDown(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles ImgEditTemplate.MouseDown
        Try
            Dim frm As New WTelecommandeNew(CBModele.Text, TxtID.Text)
            frm.ShowDialog()
            If frm.DialogResult.HasValue And frm.DialogResult.Value Then
                frm.Close()

                CBModele.ItemsSource = myService.GetListOfTemplate
                CBModele.DisplayMemberPath = "Name"
                CBModele.Visibility = Windows.Visibility.Visible

                Dim idx As Integer = 0
                If CBModele.Items IsNot Nothing And x IsNot Nothing Then
                    For Each itm In CBModele.Items
                        If itm.id IsNot Nothing Then
                            If String.IsNullOrEmpty(x.Modele) = False Then
                                If itm.id = x.Modele Then
                                    CBModele.SelectedIndex = idx
                                    Exit For
                                End If
                                idx += 1
                            End If
                        End If
                    Next
                End If
            Else
                frm.Close()
            End If

        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur ImgEditTemplate_MouseDown: " & ex.ToString, "Erreur Admin", "ImgEditTemplate_MouseDown")
        End Try
    End Sub

    Private Sub CBModele_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles CBModele.SelectionChanged
        Try
            If CbType.SelectedValue = "MULTIMEDIA" Then
                If CBModele.SelectedItem IsNot Nothing Then
                    Dim selecttemplate As HoMIDom.HoMIDom.Telecommande.Template = CBModele.SelectedItem

                    Select Case selecttemplate.Type
                        Case 0 'http
                            CbDriver.SelectedValue = "HTTP"
                            LabelAdresse1.Content = "Adresse IP"
                            If String.IsNullOrEmpty(TxtAdresse1.Text) Then TxtAdresse1.Text = "localhost"
                            LabelAdresse2.Content = "Port IP"
                        Case 1 'IR
                            CbDriver.SelectedValue = "USBuirt"
                            StkAdr1.Visibility = Windows.Visibility.Collapsed
                            StkAdr2.Visibility = Windows.Visibility.Collapsed
                        Case 2 'RS232
                            CbDriver.SelectedValue = "RS232"
                            LabelAdresse1.Content = "Port COM"
                            If My.Computer.Ports.SerialPortNames.Count > 0 Then
                                If String.IsNullOrEmpty(TxtAdresse1.Text) Then TxtAdresse1.Text = My.Computer.Ports.SerialPortNames.Item(0)
                            Else
                                TxtAdresse1.Text = "Aucun port RS232 disponible !!"
                            End If
                            LabelAdresse2.Content = "Paramètres"
                            TxtAdresse2.Text = "9600,0,8,1"
                    End Select
                End If
            End If
            IsIR()
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur CBModele_SelectionChanged: " & ex.ToString, "Erreur Admin", "CBModele_SelectionChanged")
        End Try
    End Sub

    Private Sub CbAdresse1_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles CbAdresse1.KeyUp

        Try
            'If CbAdresse1.Tag = "WEATHERMETEO" Then
            If CbAdresse1.SelectedValue <> "" And Left(CbAdresse1.SelectedValue, 4) <> "XXXX" Then TxtAdresse1.Text = CbAdresse1.SelectedValue

            'Else
            'TxtAdresse1.Text = CbAdresse1.SelectedValue
            'End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur CbAdresse1_KeyUp: " & ex.ToString, "Erreur Admin", "CbAdresse1_KeyUp")
        End Try
    End Sub

    Private Sub CbAdresse1_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles CbAdresse1.SelectionChanged

        Try

            If CbAdresse1.SelectedValue <> "" And Left(CbAdresse1.SelectedValue, 4) <> "XXXX" Then
                TxtAdresse1.Text = CbAdresse1.SelectedValue

                Dim _Driver As Object = Nothing
                Me.ForceCursor = True
                'on cherche le driver

                If CbDriver.SelectedItem IsNot Nothing Then
                    For i As Integer = 0 To ListeDrivers.Count - 1
                        If ListeDrivers.Item(i).Nom = CbDriver.SelectedItem.ToString Then
                            _Driver = myService.ReturnDriverByID(IdSrv, ListeDrivers.Item(i).ID)
                            Exit For
                        End If
                    Next
                End If

                'si on a trouvé le driver selectionné
                If _Driver IsNot Nothing Then
                    '                    MsgBox("_Driver.nom " & _Driver.nom)
                    If _Driver.LabelsDevice.Count > 0 Then
                        'permet de lier l'adresse2 au choix de l'adresse1
                        Dim tmpstr As String = Trim(Mid(CbAdresse1.SelectedValue, 1, InStr(CbAdresse1.SelectedValue, " #") + 1)) & ";"
                        Dim a() As String
                        For k As Integer = 0 To _Driver.LabelsDevice.Count - 1
                            If UCase(_Driver.LabelsDevice.Item(k).NomChamp) = "ADRESSE2" Then
                                CbAdresse2.Items.Clear()
                                If String.IsNullOrEmpty(_Driver.LabelsDevice.Item(k).Parametre) = False Then
                                    a = _Driver.LabelsDevice.Item(k).Parametre.Split("|")
                                    For g As Integer = 0 To a.Length - 1
                                        If InStr(a(g), "#;") > 0 Then  'permet de lier une valeur de adresse2 avec adresse1
                                            If (InStr(a(g), tmpstr) > 0) And (Len(tmpstr) = Len(Trim(Mid(a(g), 1, InStr(a(g), " #;") + 2)))) Then
                                                CbAdresse2.Items.Add(Trim(Mid(a(g), InStr(a(g), "#;") + 2)))
                                            End If
                                        Else
                                            CbAdresse2.Items.Add(a(g))
                                        End If
                                    Next
                                    Exit For
                                End If
                            End If
                        Next
                        Erase a
                    End If
                End If
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur CbAdresse1_SelectionChanged: " & ex.ToString, "Erreur Admin", "CbAdresse1_SelectionChanged")
        End Try
    End Sub

    Private Sub TxtAdresse1_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs) Handles TxtAdresse1.TextChanged
        Try
            If CbAdresse1.Items.Count > 0 Then
                If TxtAdresse1.Text.Trim.Length >= 8 Then 'on cherche dans la liste quand on a tapait l'ID complet
                    Dim flagTrouv As Boolean = False
                    Dim idx As Integer = 0

                    For Each item In CbAdresse1.Items
                        If TxtAdresse1.Text.ToUpper.Trim = item.ToString.ToUpper.Trim Then
                            flagTrouv = True
                            Exit For
                        End If
                        idx += 1
                    Next

                    If flagTrouv Then
                        CbAdresse1.SelectedIndex = idx
                    Else
                        CbAdresse1.Text = ""
                    End If
                End If
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur TxtAdresse1_TextChanged: " & ex.ToString, "Erreur Admin", "TxtAdresse1_TextChanged")
        End Try
    End Sub

    Private Sub CbAdresse2_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles CbAdresse2.KeyUp

        Try
            If CbAdresse2.SelectedValue <> "" And Left(CbAdresse2.SelectedValue, 4) <> "XXXX" Then TxtAdresse2.Text = CbAdresse2.SelectedValue
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur CbAdresse2_KeyUp: " & ex.ToString, "Erreur Admin", "CbAdresse2_KeyUp")
        End Try
    End Sub

    Private Sub CbAdresse2_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles CbAdresse2.SelectionChanged

        Try

            If CbAdresse2.SelectedValue <> "" And Left(CbAdresse2.SelectedValue, 4) <> "XXXX" Then TxtAdresse2.Text = CbAdresse2.SelectedValue

        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur CbAdresse2_SelectionChanged: " & ex.ToString, "Erreur Admin", "CbAdresse2_SelectionChanged")
        End Try
    End Sub

    Private Sub TxtAdresse2_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs) Handles TxtAdresse2.TextChanged
        Try
            If CbAdresse2.Items.Count > 0 Then
                If TxtAdresse2.Text.Trim.Length >= 8 Then 'on cherche dans la liste quand on a tapait l'ID complet
                    Dim flagTrouv As Boolean = False
                    Dim idx As Integer = 0

                    For Each item In CbAdresse2.Items
                        If TxtAdresse2.Text.ToUpper.Trim = item.ToString.ToUpper.Trim Then
                            '  JPHomi permet daller sur lenregistrmeent avec syntaxe approchante
                            ' If InStr(item.ToString.ToUpper.Trim, TxtAdresse2.Text.ToUpper.Trim) > 0 Then
                            flagTrouv = True
                            Exit For
                        End If
                        idx += 1
                    Next

                    If flagTrouv Then
                        CbAdresse2.SelectedIndex = idx
                    Else
                        CbAdresse2.Text = ""
                    End If
                End If
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur TxtAdresse2_TextChanged: " & ex.ToString, "Erreur Admin", "TxtAdresse2_TextChanged")
        End Try
    End Sub
#Region "Variables"

    ''' <summary>
    ''' bouton nouvelle variable
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub BtnNewVar_MouseDown(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles BtnNewVar.MouseDown
        Try
            TxtVarName.Text = ""
            TxtVarName.IsReadOnly = False
            TxtVarValue.Text = ""
            BtnNewVar.Visibility = Windows.Visibility.Collapsed
            BtnDelVar.Visibility = Windows.Visibility.Collapsed
            BtnApplyVar.Visibility = Windows.Visibility.Visible
            StkVar.Visibility = Windows.Visibility.Visible
            BtnNewVar.Tag = 1 '0=Modifier 1=New
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur BtnNewVar_MouseDown: " & ex.ToString, "Erreur Admin", "BtnNewVar_MouseDown")
        End Try
    End Sub

    ''' <summary>
    ''' Bouton supprimer variable
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub BtnDelVar_MouseDown(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles BtnDelVar.MouseDown
        Try
            If IsNothing(_ListVar) Then Exit Sub

            If CbVariables.SelectedIndex >= 0 Then
                If _ListVar.ContainsKey(CbVariables.SelectedItem.key) Then
                    _ListVar.Remove(CbVariables.SelectedItem.key)
                    Refresh_cbVar()
                    StkVar.Visibility = Windows.Visibility.Collapsed
                    BtnApplyVar.Visibility = Windows.Visibility.Collapsed
                    BtnNewVar.Visibility = Windows.Visibility.Visible
                    BtnDelVar.Visibility = Windows.Visibility.Visible
                    BtnNewVar.Tag = Nothing
                Else
                    MessageBox.Show("Le nom de cette variable n'existe pas!", "Erreur", MessageBoxButton.OK, MessageBoxImage.Exclamation)
                End If
            Else
                MessageBox.Show("Veuillez sélectionner une variable à supprimer", "Erreur", MessageBoxButton.OK, MessageBoxImage.Exclamation)
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur BtnDelVar_MouseDown: " & ex.ToString, "Erreur Admin", "BtnDelVar_MouseDown")
        End Try
    End Sub

    ''' <summary>
    ''' Bouton appliquer variable
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Private Sub BtnApplyVar_MouseDown(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles BtnApplyVar.MouseDown
        Try

            If IsNothing(_ListVar) Then Exit Sub

            If BtnNewVar.Tag = 0 Then 'Modification de variable
                If _ListVar.ContainsKey(TxtVarName.Text) Then
                    _ListVar(TxtVarName.Text) = TxtVarValue.Text
                    Refresh_cbVar()
                Else
                    MessageBox.Show("Le nom de cette variable n'existe pas!", "Erreur", MessageBoxButton.OK, MessageBoxImage.Exclamation)
                End If
            Else 'Nouvelle variable
                If String.IsNullOrEmpty(TxtVarName.Text) Then Exit Sub
                If Not _ListVar.ContainsKey(TxtVarName.Text) Then
                    _ListVar.Add(TxtVarName.Text, TxtVarValue.Text)
                    Refresh_cbVar()
                Else
                    MessageBox.Show("Erreur cette variable existe déjà!!", "Erreur", MessageBoxButton.OK, MessageBoxImage.Exclamation)
                End If
            End If
            BtnApplyVar.Visibility = Windows.Visibility.Collapsed
            BtnNewVar.Visibility = Windows.Visibility.Visible
            BtnNewVar.Tag = Nothing
            BtnDelVar.Visibility = Windows.Visibility.Visible
            StkVar.Visibility = Windows.Visibility.Collapsed
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur BtnApplyVar_MouseDown: " & ex.ToString, "Erreur Admin", "BtnApplyVar_MouseDown")
        End Try
    End Sub

    Private Sub Refresh_cbVar()
        Try

            If IsNothing(_ListVar) Then Exit Sub

            If _ListVar.Count > 0 Then
                CbVariables.ItemsSource = New Forms.BindingSource(_ListVar, Nothing)
                CbVariables.DisplayMemberPath = "Key"
                CbVariables.SelectedValuePath = "Value"
            Else
                CbVariables.ItemsSource = Nothing

            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur Refresh_cbVar: " & ex.ToString, "Erreur Admin", "Refresh_cbVar")
        End Try
    End Sub


    Private Sub CbVariables_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles CbVariables.SelectionChanged
        Try
            If IsNothing(_ListVar) Then Exit Sub

            If CbVariables.SelectedItem IsNot Nothing Then
                TxtVarName.Text = CbVariables.SelectedItem.key
                TxtVarName.IsReadOnly = True
                TxtVarValue.Text = _ListVar(TxtVarName.Text)
                StkVar.Visibility = Windows.Visibility.Visible
                BtnApplyVar.Visibility = Windows.Visibility.Visible
                BtnNewVar.Visibility = Windows.Visibility.Visible
                BtnDelVar.Visibility = Windows.Visibility.Visible
                BtnNewVar.Tag = 0 '0=Modifier 1=New
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur CbVariables_MouseUp: " & ex.ToString, "Erreur Admin", "CbVariables_MouseUp")
        End Try
    End Sub

#End Region


    Private Sub BtnLearn1_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles BtnLearn1.Click
        Try
            If MessageBox.Show("Merci de rester appuyer sur le bouton de votre télécommande jusqu'à inscription d'un message dans le champs", "Info", MessageBoxButton.OKCancel, MessageBoxImage.Information) = MessageBoxResult.OK Then
                If _Driver IsNot Nothing Then
                    If String.IsNullOrEmpty(_Driver.ID) = False Then
                        Me.Cursor = Cursors.Wait
                        Dim retourlearn As String = myService.StartLearning(IdSrv, _Driver.ID)
                        Me.Cursor = Nothing
                        If retourlearn = "" Or retourlearn.Substring(0, 4) = "ERR:" Then
                            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur durant l'apprentissage: " & retourlearn, "Erreur Admin", "BtnLearn1_Click")
                        Else
                            TxtAdresse1.Text = retourlearn
                        End If
                    Else
                        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur Apprendre1: Driver non affecté, vérifier qu'un driver à bien été associé au composant et que celui-ci a été créé", "Erreur Admin", "BtnLearn1_Click")
                    End If
                Else
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur Apprendre1: Driver non affecté, vérifier qu'un driver à bien été associé au composant et que celui-ci a été créé", "Erreur Admin", "BtnLearn1_Click")
                End If
            Else
                Exit Sub
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur BtnLearn1_Click: " & ex.ToString, "Erreur Admin", "BtnLearn1_Click")
        End Try
    End Sub

    Private Sub BtnLearn2_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles BtnLearn2.Click
        Try
            If MessageBox.Show("Merci de rester appuyer sur le bouton de votre télécommande jusqu'à inscription d'un message dans le champs", "Info", MessageBoxButton.OKCancel, MessageBoxImage.Information) = MessageBoxResult.OK Then
                If _Driver IsNot Nothing Then
                    If String.IsNullOrEmpty(_Driver.ID) = False Then
                        Me.Cursor = Cursors.Wait
                        Dim retourlearn As String = myService.StartLearning(IdSrv, _Driver.ID)
                        Me.Cursor = Nothing
                        If retourlearn = "" Or retourlearn.Substring(0, 4) = "ERR:" Then
                            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur durant l'apprentissage: " & retourlearn, "Erreur Admin", "BtnLearn2_Click")
                        Else
                            TxtAdresse2.Text = retourlearn
                        End If
                    Else
                        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur Apprendre1: Driver non affecté, vérifier qu'un driver à bien été associé au composant et que celui-ci a été créé", "Erreur Admin", "BtnLearn2_Click")
                    End If
                Else
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur Apprendre1: Driver non affecté, vérifier qu'un driver à bien été associé au composant et que celui-ci a été créé", "Erreur Admin", "BtnLearn2_Click")
                End If
            Else
                Exit Sub
            End If
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Erreur BtnLearn1_Click: " & ex.ToString, "Erreur Admin", "BtnLearn2_Click")
        End Try
    End Sub
    Function SaveDevice() As Boolean
        Try
            Dim retour As String = ""
            Dim _driverid As String = ""

            'on recupere le DriverID depuis le combobox
            For i As Integer = 0 To myService.GetAllDrivers(IdSrv).Count - 1
                If myService.GetAllDrivers(IdSrv).Item(i).Nom = CbDriver.Text Then
                    _driverid = myService.GetAllDrivers(IdSrv).Item(i).ID
                    Exit For
                End If
            Next

            'on corrige certaines valeurs
            TxtRefresh.Text = Regex.Replace(TxtRefresh.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
            TxtPrecision.Text = Regex.Replace(TxtPrecision.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
            TxtCorrection.Text = Regex.Replace(TxtCorrection.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
            TxtValDef.Text = Regex.Replace(TxtValDef.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
            TxtValueMax.Text = Regex.Replace(TxtValueMax.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
            TxtValueMin.Text = Regex.Replace(TxtValueMin.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)

            'on check les valeurs renseignées
            If Left(TxtNom.Text, 5) <> "HOMI_" Then
                If (String.IsNullOrEmpty(TxtNom.Text) Or HaveCaractSpecial(TxtNom.Text)) Then
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le nom du composant doit être renseigné et ne doit pas comporter de caractère spécial", "Erreur", "SaveDevice")
                    Return False
                End If
            End If
            If String.IsNullOrEmpty(CbDriver.Text) = True Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le driver du composant est obligatoire !!", "Erreur", "SaveDevice")
                Return False
            End If
            If String.IsNullOrEmpty(CbType.Text) = True Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le type du composant est obligatoire !!", "Erreur", "SaveDevice")
                Return False
            End If
            If String.IsNullOrEmpty(TxtAdresse1.Text) = True Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "L'adresse de base du composant est obligatoire !!", "Erreur", "SaveDevice")
                Return False
            End If
            retour = myService.VerifChamp(IdSrv, _driverid, "ADRESSE1", TxtAdresse1.Text)
            If retour <> "0" Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Champ " & LabelAdresse1.Content & ": " & retour, "Erreur", "SaveDevice")
                Return False
            End If
            retour = myService.VerifChamp(IdSrv, _driverid, "ADRESSE2", TxtAdresse2.Text)
            If retour <> "0" Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Champ " & LabelAdresse2.Content & ": " & retour, "Erreur", "SaveDevice")
                Return False
            End If

            'on recupere le bon champ Modele : Combobox ou texte
            Dim _modele As String = ""
            If CBModele.Tag = 1 Then
                _modele = CBModele.Text
            Else
                If TxtModele.Tag = 1 Then
                    _modele = TxtModele.Text
                Else
                    _modele = ""
                End If
            End If

            'on crée le dictionnaire parametre à passer à savedevice
            Dim Proprietes As New Dictionary(Of String, String)
            'recuperation, verification et correction des valeurs pour LAMPERGBW
            If CbType.Text = "LAMPERGBW" Then
                If IsNumeric(TxtRGBWred.Text) = False Or (TxtRGBWred.Text < 0 Or TxtRGBWred.Text > 255) Then
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW Rouge doit être un Nombre compris entre 0 et 255", "Erreur", "SaveDevice")
                    Return False
                End If
                If IsNumeric(TxtRGBWgreen.Text) = False Or (TxtRGBWgreen.Text < 0 Or TxtRGBWgreen.Text > 255) Then
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW green doit être un Nombre compris entre 0 et 255", "Erreur", "SaveDevice")
                    Return False
                End If
                If IsNumeric(TxtRGBWblue.Text) = False Or (TxtRGBWblue.Text < 0 Or TxtRGBWblue.Text > 255) Then
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW blue doit être un Nombre compris entre 0 et 255", "Erreur", "SaveDevice")
                    Return False
                End If
                If IsNumeric(TxtRGBWwhite.Text) = False Or (TxtRGBWwhite.Text < 0 Or TxtRGBWwhite.Text > 255) Then
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW white doit être un Nombre compris entre 0 et 255", "Erreur", "SaveDevice")
                    Return False
                End If
                If IsNumeric(TxtRGBWspeed.Text) = False Or (TxtRGBWspeed.Text < 0 Or TxtRGBWspeed.Text > 100) Then
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW Speed doit être un Nombre compris entre 0 et 100", "Erreur", "SaveDevice")
                    Return False
                End If
                Proprietes.Add("red", TxtRGBWred.Text)
                Proprietes.Add("green", TxtRGBWgreen.Text)
                Proprietes.Add("blue", TxtRGBWblue.Text)
                Proprietes.Add("white", TxtRGBWwhite.Text)
                Proprietes.Add("temperature", TxtRGBWtemperature.Text)
                Proprietes.Add("speed", TxtRGBWspeed.Text)
                Proprietes.Add("optionnal", TxtRGBWoptionnal.Text)
            End If


            'on sauvegarde le composant
            If CbType.Text = "MULTIMEDIA" Then
                If CBModele.SelectedItem IsNot Nothing Then
                    _modele = CBModele.SelectedItem.ID
                Else
                    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez sélectionner ou ajouter un template au device!", "Erreur", "SaveDevice")
                    Return False
                End If

                If _Action = EAction.Modifier Then
                    If x IsNot Nothing Then retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, x.Commandes, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
                Else
                    retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, Nothing, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
                End If
            Else
                retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, Nothing, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
            End If

            If retour = "98" Then
                AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le nom du device: " & TxtNom.Text & " existe déjà impossible de l'enregister", "ERREUR", "SaveDevice")
                Return False
            End If

            'on affiche l'ID du composant (si c'était un nouveau composant, il n'y avait pas encore d'ID)
            TxtID.Text = retour

            VerifDriver(_driverid)
            If String.IsNullOrEmpty(_DeviceId) = True Then _DeviceId = retour
            SaveInZone()
            FlagChange = True

            Return True
        Catch ex As Exception
            AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Function DeviceSave: " & ex.Message, "ERREUR", "SaveDevice")
            Return False
        End Try
    End Function

End Class

'=========================================================================================================================================================
'JpHomi => CODE EN COMMENTAIRE
'=========================================================================================================================================================
'Private Sub BtnOK_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnOK.Click
'   Try
'     Dim retour As String = ""
'         Dim _driverid As String = ""

'on recupere le DriverID depuis le combobox
'         For i As Integer = 0 To myService.GetAllDrivers(IdSrv).Count - 1
'If myService.GetAllDrivers(IdSrv).Item(i).Nom = CbDriver.Text Then
'_driverid = myService.GetAllDrivers(IdSrv).Item(i).ID
'          Exit For
'          End If
'          Next
'
'on corrige certains valeurs
'TxtRefresh.Text = Replace(TxtRefresh.Text, ".", ",")
'          TxtRefresh.Text = Regex.Replace(TxtRefresh.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtRefresh.Text = Replace(TxtRefresh.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'          TxtPrecision.Text = Regex.Replace(TxtPrecision.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtPrecision.Text = Replace(TxtPrecision.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'         TxtCorrection.Text = Regex.Replace(TxtCorrection.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtCorrection.Text = Replace(TxtCorrection.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'        TxtValDef.Text = Regex.Replace(TxtValDef.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtValDef.Text = Replace(TxtValDef.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'       TxtValueMax.Text = Regex.Replace(TxtValueMax.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtValueMax.Text = Replace(TxtValueMax.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'       TxtValueMin.Text = Regex.Replace(TxtValueMin.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtValueMin.Text = Replace(TxtValueMin.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)

'on check les valeurs renseignés
'        If Left(TxtNom.Text, 5) <> "HOMI_" Then
'If (String.IsNullOrEmpty(TxtNom.Text) Or HaveCaractSpecial(TxtNom.Text)) Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le nom du composant doit être renseigné et ne doit pas comporter de caractère spécial", "Erreur", "")
'           Exit Sub
'           End If
'           End If
'           If IsNumeric(TxtPrecision.Text) = False Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Précision doit être un Nombre", "Erreur", "")
'           Exit Sub
'           End If
'           ' Le champ correction peut contenir des symboles mathematiques (*/+-)
'If IsNumeric(TxtCorrection.Text) = False Then
'AfficheMessageAndLog (HoMIDom.HoMIDom.Server.TypeLog.ERREUR,"Le champ Correction doit être un Nombre", "Erreur","")
'  Exit Sub
' End If
'           If IsNumeric(TxtValDef.Text) = False Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Valeur Defaut doit être un Nombre", "Erreur", "")
'           Exit Sub
'           End If
'           If IsNumeric(TxtValueMax.Text) = False Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Valeur Max doit être un Nombre", "Erreur", "")
'          Exit Sub
'          End If
'          If IsNumeric(TxtValueMin.Text) = False Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Valeur Min doit être un Nombre", "Erreur", "")
'           Exit Sub
'         End If
'          If String.IsNullOrEmpty(TxtNom.Text) = True Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le nom du composant est obligatoire !!", "Erreur", "")
'          Exit Sub
'          End If
'          If String.IsNullOrEmpty(CbType.Text) = True Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le type du composant est obligatoire !!", "Erreur", "")
'          Exit Sub
'          End If
'          If String.IsNullOrEmpty(CbDriver.Text) = True Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le driver du composant est obligatoire !!", "Erreur", "")
'          Exit Sub
'          End If
'          If String.IsNullOrEmpty(TxtAdresse1.Text) = True Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "L'adresse de base du composant est obligatoire !!", "Erreur", "")
'           Exit Sub
'          End If
'          retour = myService.VerifChamp(IdSrv, _driverid, "ADRESSE1", TxtAdresse1.Text)
'          If retour <> "0" Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Champ " & LabelAdresse1.Content & ": " & retour, "Erreur", "")
'          Exit Sub
'          End If
'          retour = myService.VerifChamp(IdSrv, _driverid, "ADRESSE2", TxtAdresse2.Text)
'          If retour <> "0" Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Champ " & LabelAdresse2.Content & ": " & retour, "Erreur", "")
'         Exit Sub
'         End If

'on recupere le bon champ Modele : Combobox ou texte
'         Dim _modele As String = ""
'         If CBModele.Tag = 1 Then
'_modele = CBModele.Text
'         Else
'         If TxtModele.Tag = 1 Then
'_modele = TxtModele.Text
'         Else
'         _modele = ""
'         End If
'         End If

'on cré le dictionnaire parametre à passer à savedevice
'        Dim Proprietes As New Dictionary(Of String, String)
'recuperation, verification et correction des valeurs pour LAMPERGBW
'         If CbType.Text = "LAMPERGBW" Then
'If IsNumeric(TxtRGBWred.Text) = False Or (TxtRGBWred.Text < 0 Or TxtRGBWred.Text > 255) Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW Rouge doit être un Nombre compris entre 0 et 255", "Erreur", "")
'          Exit Sub
'           End If
'          If IsNumeric(TxtRGBWgreen.Text) = False Or (TxtRGBWgreen.Text < 0 Or TxtRGBWgreen.Text > 255) Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW green doit être un Nombre compris entre 0 et 255", "Erreur", "")
'          Exit Sub
'           End If
'          If IsNumeric(TxtRGBWblue.Text) = False Or (TxtRGBWblue.Text < 0 Or TxtRGBWblue.Text > 255) Then
'AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW blue doit être un Nombre compris entre 0 et 255", "Erreur", "")
'Exit Sub
'End If
'If IsNumeric(TxtRGBWwhite.Text) = False Or (TxtRGBWwhite.Text < 0 Or TxtRGBWwhite.Text > 255) Then
'    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW white doit être un Nombre compris entre 0 et 255", "Erreur", "")
'    Exit Sub
'End If
'If IsNumeric(TxtRGBWspeed.Text) = False Or (TxtRGBWspeed.Text < 0 Or TxtRGBWspeed.Text > 100) Then
'    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW Speed doit être un Nombre compris entre 0 et 100", "Erreur", "")
'    Exit Sub
'End If
'Proprietes.Add("red", TxtRGBWred.Text)
'Proprietes.Add("green", TxtRGBWgreen.Text)
'Proprietes.Add("blue", TxtRGBWblue.Text)
'Proprietes.Add("white", TxtRGBWwhite.Text)
'Proprietes.Add("temperature", TxtRGBWtemperature.Text)
'Proprietes.Add("speed", TxtRGBWspeed.Text)
'Proprietes.Add("optionnal", TxtRGBWoptionnal.Text)
'End If


''on sauvegarde le composant
'If CbType.Text = "MULTIMEDIA" Then
'    If CBModele.SelectedItem IsNot Nothing Then
'        _modele = CBModele.SelectedItem.ID
'    Else
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez sélectionner ou ajouter un template au device!", "Erreur", "")
'        Exit Sub
'    End If

'    'If x IsNot Nothing Then
'    '    If String.IsNullOrEmpty(x.Modele) = True Then

'    '    End If
'    '    _modele = x.Modele
'    'End If
'    If _Action = EAction.Modifier Then
'        If x IsNot Nothing Then retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, x.Commandes, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
'    Else
'        retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, Nothing, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
'    End If
'Else
'    retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, Nothing, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
'End If

'If retour = "98" Then
'    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le nom du device: " & TxtNom.Text & " existe déjà impossible de l'enregister", "ERREUR", "")
'    Exit Sub
'End If

''on affiche l'ID du composant (si c'était un nouveau composant, il n'y avait pas encore d'ID)
'TxtID.Text = retour

'VerifDriver(_driverid)
'If String.IsNullOrEmpty(_DeviceId) = True Then _DeviceId = retour
'SaveInZone()
'FlagChange = True

'If _Action = EAction.Nouveau And NewDevice IsNot Nothing And flagnewdev Then
'    myService.DeleteNewDevice(IdSrv, NewDevice.ID)
'    NewDevice = Nothing
'    flagnewdev = False
'End If


'           RaiseEvent CloseMe(Me, False)

'  Catch ex As Exception
'     AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Sub uDevice BtnSve and Close: " & ex.ToString, "ERREUR", "")
' End Try
'End Sub
'Private Sub BtnSave_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnSave.Click
'   Try

'Dim retour As String = ""
'Dim _driverid As String = ""

''on recupere le DriverID depuis le combobox
'For i As Integer = 0 To myService.GetAllDrivers(IdSrv).Count - 1
'    If myService.GetAllDrivers(IdSrv).Item(i).Nom = CbDriver.Text Then
'        _driverid = myService.GetAllDrivers(IdSrv).Item(i).ID
'        Exit For
'    End If
'Next

''on corrige certains valeurs
''TxtRefresh.Text = Replace(TxtRefresh.Text, ".", ",")
'TxtRefresh.Text = Regex.Replace(TxtRefresh.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
''TxtRefresh.Text = Replace(TxtRefresh.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtPrecision.Text = Regex.Replace(TxtPrecision.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
''TxtPrecision.Text = Replace(TxtPrecision.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtCorrection.Text = Regex.Replace(TxtCorrection.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
''TxtCorrection.Text = Replace(TxtCorrection.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtValDef.Text = Regex.Replace(TxtValDef.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
''TxtValDef.Text = Replace(TxtValDef.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtValueMax.Text = Regex.Replace(TxtValueMax.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
''TxtValueMax.Text = Replace(TxtValueMax.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
'TxtValueMin.Text = Regex.Replace(TxtValueMin.Text, "[.,]", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
''TxtValueMin.Text = Replace(TxtValueMin.Text, ",", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)

''on check les valeurs renseignés
'If CbType.Text = "BAROMETRE" _
'    Or CbType.Text = "COMPTEUR" _
'    Or CbType.Text = "ENERGIEINSTANTANEE" _
'    Or CbType.Text = "ENERGIETOTALE" _
'    Or CbType.Text = "GENERIQUEVALUE" _
'    Or CbType.Text = "HUMIDITE" _
'    Or CbType.Text = "LAMPE" _
'    Or CbType.Text = "LAMPERGBW" _
'    Or CbType.Text = "PLUIECOURANT" _
'    Or CbType.Text = "PLUIETOTAL" _
'    Or CbType.Text = "TEMPERATURE" _
'    Or CbType.Text = "TEMPERATURECONSIGNE" _
'    Or CbType.Text = "VITESSEVENT" _
'    Or CbType.Text = "UV" _
'    Or CbType.Text = "VOLET" _
'    Then
'    If IsNumeric(TxtPrecision.Text) = False Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Précision doit être un Nombre", "Erreur", "")
'        Exit Sub
'    End If
'    ' Le champ correction peut contenir des symboles mathematiques (*/+-)
'    'If IsNumeric(TxtCorrection.Text) = False Then
'    '    AfficheMessageAndLog (HoMIDom.HoMIDom.Server.TypeLog.ERREUR,"Le champ Correction doit être un Nombre", "Erreur","")
'    '    Exit Sub
'    'End If
'    If IsNumeric(TxtValDef.Text) = False Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Valeur Defaut doit être un Nombre", "Erreur", "")
'        Exit Sub
'    End If
'    If IsNumeric(TxtValueMax.Text) = False Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Valeur Max doit être un Nombre", "Erreur", "")
'        Exit Sub
'    End If
'    If IsNumeric(TxtValueMin.Text) = False Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ Valeur Min doit être un Nombre", "Erreur", "")
'        Exit Sub
'    End If
'End If
'If Left(TxtNom.Text, 5) <> "HOMI_" Then
'    If (String.IsNullOrEmpty(TxtNom.Text) Or HaveCaractSpecial(TxtNom.Text)) Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le nom du composant doit être renseigné et ne doit pas comporter de caractère spécial", "Erreur", "")
'        Exit Sub
'    End If
'End If
'If String.IsNullOrEmpty(CbType.Text) = True Then
'    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le type du device est obligatoire !!", "Erreur", "")
'    Exit Sub
'End If
'If String.IsNullOrEmpty(CbDriver.Text) = True Then
'    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le driver du device est obligatoire !!", "Erreur", "")
'    Exit Sub
'End If
'If String.IsNullOrEmpty(TxtAdresse1.Text) = True Then
'    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "L'adresse de base du device est obligatoire !!", "Erreur", "")
'    Exit Sub
'End If

''on recupere le bon champ Modele : Combobox ou texte
'Dim _modele As String
'If CBModele.Tag = 1 Then
'    _modele = CBModele.Text
'Else
'    If TxtModele.Tag = 1 Then
'        _modele = TxtModele.Text
'    Else
'        _modele = ""
'    End If
'End If

''on cré le dictionnaire parametre à passer à savedevice
'Dim Proprietes As New Dictionary(Of String, String)
''recuperation, verification et correction des valeurs pour LAMPERGBW
'If CbType.Text = "LAMPERGBW" Then
'    If IsNumeric(TxtRGBWred.Text) = False Or (TxtRGBWred.Text < 0 Or TxtRGBWred.Text > 255) Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW Rouge doit être un Nombre compris entre 0 et 255", "Erreur", "")
'        Exit Sub
'    End If
'    If IsNumeric(TxtRGBWgreen.Text) = False Or (TxtRGBWgreen.Text < 0 Or TxtRGBWgreen.Text > 255) Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW green doit être un Nombre compris entre 0 et 255", "Erreur", "")
'        Exit Sub
'    End If
'    If IsNumeric(TxtRGBWblue.Text) = False Or (TxtRGBWblue.Text < 0 Or TxtRGBWblue.Text > 255) Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW blue doit être un Nombre compris entre 0 et 255", "Erreur", "")
'        Exit Sub
'    End If
'    If IsNumeric(TxtRGBWwhite.Text) = False Or (TxtRGBWwhite.Text < 0 Or TxtRGBWwhite.Text > 255) Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW white doit être un Nombre compris entre 0 et 255", "Erreur", "")
'        Exit Sub
'    End If
'    If IsNumeric(TxtRGBWspeed.Text) = False Or (TxtRGBWspeed.Text < 0 Or TxtRGBWspeed.Text > 100) Then
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le champ RGBW Speed doit être un Nombre compris entre 0 et 100", "Erreur", "")
'        Exit Sub
'    End If
'    Proprietes.Add("red", TxtRGBWred.Text)
'    Proprietes.Add("green", TxtRGBWgreen.Text)
'    Proprietes.Add("blue", TxtRGBWblue.Text)
'    Proprietes.Add("white", TxtRGBWwhite.Text)
'    Proprietes.Add("temperature", TxtRGBWtemperature.Text)
'    Proprietes.Add("speed", TxtRGBWspeed.Text)
'    Proprietes.Add("optionnal", TxtRGBWoptionnal.Text)
'End If

''on sauvegarde le composant
'If CbType.Text = "MULTIMEDIA" Then
'    If CBModele.SelectedItem IsNot Nothing Then
'        _modele = CBModele.SelectedItem.ID
'    Else
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Veuillez sélectionner ou ajouter un template au device!", "Erreur", "")
'        Exit Sub
'    End If
'    If _Action = EAction.Modifier Then
'        If x IsNot Nothing Then retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, x.Commandes, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
'    Else
'        retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, Nothing, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
'    End If
'Else
'    retour = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, ChkHisto.IsChecked, TxtRefreshHisto.Text, TxtPurge.Text, TxtMoyJour.Text, TxtMoyHeure.Text, TxtAdresse2.Text, ImgDevice.Tag, _modele, TxtDescript.Text, TxtLastChangeDuree.Text, ChKLastEtat.IsChecked, TxtCorrection.Text, TxtFormatage.Text, TxtPrecision.Text, TxtValueMax.Text, TxtValueMin.Text, TxtValDef.Text, Nothing, TxtUnit.Text, TxtPuissance.Text, ChKAllValue.IsChecked, _ListVar, Proprietes)
'End If
'If retour = "98" Then
'    AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "Le nom du device: " & TxtNom.Text & " existe déjà impossible de l'enregister", "ERREUR", "")
'    Exit Sub
'End If

''on affiche l'ID du composant (si c'était un nouveau composant, il n'y avait pas encore d'ID)
'TxtID.Text = retour

'VerifDriver(_driverid)
'If String.IsNullOrEmpty(_DeviceId) = True Then _DeviceId = retour
'SaveInZone()
'FlagChange = True

'If _Action = EAction.Nouveau And NewDevice IsNot Nothing And flagnewdev Then
'    myService.DeleteNewDevice(IdSrv, NewDevice.ID)
'    NewDevice = Nothing
'    flagnewdev = False
'End If

''Dim uid As String = myService.SaveDevice(IdSrv, _DeviceId, TxtNom.Text, TxtAdresse1.Text, ChkEnable.IsChecked, ChKSolo.IsChecked, _driverid, CbType.Text, TxtRefresh.Text, TxtAdresse2.Text, ImgDevice.Tag, CBModele.Text, TxtDescript.Text, TxtLastChangeDuree.Text)


'        BtnTest.Visibility = Windows.Visibility.Visible
'        BtnHisto.Visibility = Windows.Visibility.Visible
'        If CbType.SelectedValue = "MULTIMEDIA" Then
'            BtnEditTel.Visibility = Windows.Visibility.Visible
'            TxtModele.Visibility = Visibility.Hidden
'            LabelModele.Visibility = Windows.Visibility.Hidden
'        End If

'        If _DeviceId.Length > 3 Then x = myService.ReturnDeviceByID(IdSrv, _DeviceId)
'        End If

'    Catch ex As Exception
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR Sub uDevice BtnSave_Click: " & ex.Message, "ERREUR", "")
'    End Try
'End Sub

'Private Sub BtnEditTel_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BtnEditTel.Click
'    Try
'        Dim _driverid As String = ""
'        For i As Integer = 0 To myService.GetAllDrivers(IdSrv).Count - 1
'            If myService.GetAllDrivers(IdSrv).Item(i).Nom = CbDriver.Text Then
'                _driverid = myService.GetAllDrivers(IdSrv).Item(i).ID
'                Exit For
'            End If
'        Next

'        Dim frm As New WTelecommande(_DeviceId, _driverid, x)
'        frm.ShowDialog()
'        If frm.DialogResult.HasValue And frm.DialogResult.Value Then
'            If x IsNot Nothing Then
'                If String.IsNullOrEmpty(x.Modele) = False Then 'On vérifie si on viens de changer de template
'                    'If x.Commandes.Count = 0 The
'                    'BtnEditTel.Visibility = Windows.Visibility.Collapsed
'                End If
'                frm.Close()
'            End If
'        Else
'            frm.Close()
'        End If
'    Catch ex As Exception
'        AfficheMessageAndLog(HoMIDom.HoMIDom.Server.TypeLog.ERREUR, "ERREUR BtnEditTel_MouseDown: " & ex.ToString, "ERREUR", "")
'    End Try
'End Sub

