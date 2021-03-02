/***
This a big script that controls the logic of a match
in a chess game. The game also had variations of a 
regular match rules, so it may have exceptions depending
on the rules applied.
***/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameLogic : MonoBehaviour
{
    [SerializeField]
    bool whiteTurn = true;
    [SerializeField]
    TextMeshProUGUI playerTurnText;
    [SerializeField]
    TextMeshProUGUI checkText;
    [SerializeField]
    GameObject endPanel;
    [SerializeField]
    TextMeshProUGUI winnerText;
    bool finished;
    Timer timer;

    Figure blackKing;
    Figure whiteKing;
    [SerializeField]
    Figure figureCheckingAtTheMoment;
    [SerializeField]
    bool whiteCheck;
    [SerializeField]
    bool blackCheck;
    [SerializeField]
    bool coronating;
    [SerializeField]
    bool passantAvailable;
    [SerializeField]
    Figure passantFigure;


    //DOUBLE TURN MODIFIFER
    int turnsLeft = 2;


    [Header("Figure prefabs")]
    public GameObject pawnPrefab;
    public GameObject towerPrefab;
    public GameObject beshopPrefab;
    public GameObject queenPrefab;
    public GameObject knightPrefab;
    public GameObject kingPrefab;

    public void SelectFigure(Figure figure)
    {
        if(figure == figureCheckingAtTheMoment)
        {
            figureCheckingAtTheMoment = null;
            return;
        }

        if(figure.white == whiteTurn)
        {
            figureCheckingAtTheMoment = figure;
        }
    }
    public bool LegalMove(Figure figure, Cell from, Cell to)
    {
        if (figure.IsKing())
            return LegalKingMove(figure, from, to);

        Figure king = figure.white ? whiteKing : blackKing;

        Vector2[] moves = { new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(1, 0),
        new Vector2(1, -1),
        new Vector2(0, -1),
        new Vector2(-1, -1),
        new Vector2(-1, 0),
        new Vector2(-1, 1)};

        for (int i = 0; i < moves.Length; i++)
        {
            int offset = 1;

            while (true)
            {
                Cell cell = Board.instance.GetCell(king.GetCell().GetIndex() + moves[i] * offset);
                if (cell == null)
                    break;

                if (cell.GetIndex() == from.GetIndex())
                {
                    offset++;
                    continue;
                }

                if (cell.GetIndex() == to.GetIndex())
                {
                    break;
                }

                if (cell.HasFigureOnIt())
                {
                    if(cell.FigureOnIt().white == king.white)
                    {
                        break;
                    }
                    else
                    {
                        if(cell.FigureOnIt().GetComponent<TowerMovement>() != null) // Tower check
                        {
                            if(i == 0 || i == 2 || i == 4 || i == 6)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (cell.FigureOnIt().GetComponent<BeshopMovement>() != null) // Bishop check
                        {
                            if (i == 1 || i == 3 || i == 5 || i == 7)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (cell.FigureOnIt().GetComponent<QueenMovement>() != null) // Queen check
                        {
                            return false;
                        }
                        if (cell.FigureOnIt().GetComponent<KingMovement>() != null) // King check
                        {
                            if (offset == 1)
                                return false;
                            else
                                break;
                        }
                    }
                    break;
                }
                offset++;
            }
        }

        Vector2[] knightMoves = { new Vector2(1, 2),
        new Vector2(2, 1),
        new Vector2(2, -1),
        new Vector2(1, -2),
        new Vector2(-1, -2),
        new Vector2(-2, -1),
        new Vector2(-2, 1),
        new Vector2(-1, 2)};

        for (int i = 0; i < knightMoves.Length; i++)
        {
            Cell cell = Board.instance.GetCell(king.GetCell().GetIndex() + knightMoves[i]);
            if (cell == null)
                continue;

            if (cell != to && cell.HasFigureOnIt() && cell.FigureOnIt().white != king.white && cell.FigureOnIt().GetComponent<KnightMovement>() != null)
            {
                return false;
            }
        }

        if (king.white) //PAWN
        {
            Cell pawnPos0 = Board.instance.GetCell(king.GetCell().GetIndex() + new Vector2(1, 1));
            if (pawnPos0 != to && pawnPos0 != null && pawnPos0.HasFigureOnIt() && pawnPos0.FigureOnIt().white != king.white && pawnPos0.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
            Cell pawnPos1 = Board.instance.GetCell(king.GetCell().GetIndex() + new Vector2(-1, 1));
            if (pawnPos1 != to && pawnPos1 != null && pawnPos1.HasFigureOnIt() && pawnPos1.FigureOnIt().white != king.white && pawnPos1.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
        }
        else
        {
            Cell pawnPos0 = Board.instance.GetCell(king.GetCell().GetIndex() + new Vector2(1, -1));
            if (pawnPos0 != to && pawnPos0 != null && pawnPos0.HasFigureOnIt() && pawnPos0.FigureOnIt().white != king.white && pawnPos0.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
            Cell pawnPos1 = Board.instance.GetCell(king.GetCell().GetIndex() + new Vector2(-1, -1));
            if (pawnPos1 != to && pawnPos1 != null && pawnPos1.HasFigureOnIt() && pawnPos1.FigureOnIt().white != king.white && pawnPos1.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
        }

        return true;
    }
    public bool CheckCheck(bool whiteMove)
    {
        if (whiteMove)
        {
            if (blackKing == null)
                return false;
            return !LegalKingMove(blackKing, null, blackKing.GetCell());
        }
        else
        {
            if (whiteKing == null)
                return false;
            return !LegalKingMove(whiteKing, null, whiteKing.GetCell());
        }
    }
    public bool CheckCheckMate(bool whiteMove)
    {
        foreach (var figure in FindObjectsOfType<Figure>())
        {
            if(figure.white != whiteMove)
            {
                foreach (var move in figure.GetComponent<FigureMovement>().GetPotentialPositions())
                {
                    if(LegalMove(figure, figure.GetCell(), Board.instance.GetCell(move)))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    public bool TieDueLackOfFigures()
    {
        int bishopCount = 0;
        int knightCount = 0;
        List<Figure> bishops = new List<Figure>();

        foreach (var figure in FindObjectsOfType<Figure>())
        {
            if (figure.GetComponent<PawnMovement>() != null)
                return false;
            if (figure.GetComponent<TowerMovement>() != null)
                return false;
            if (figure.GetComponent<QueenMovement>() != null)
                return false;

            if (figure.GetComponent<KnightMovement>() != null)
                knightCount++;
            if (figure.GetComponent<BeshopMovement>() != null)
            {
                bishops.Add(figure);
                bishopCount++;
            }
        }

        if (bishopCount == 0 && knightCount == 0)
            return true;
        if (bishopCount == 0 && knightCount == 1)
            return true;
        if (bishopCount == 1 && knightCount == 0)
            return true;
        if (bishopCount == 0 && knightCount == 2)
            return true;

        if (bishopCount > 2)
            return false;

        if (knightCount > 2)
            return false;

        if(bishopCount == 2)
        {
            foreach (var item in bishops)
            {
                if (bishops[0].white == bishops[1].white)
                    return false;
            }
        }

        return true;
    }
    public bool LegalKingMove(Figure figure, Cell from, Cell to)
    {
        Figure king = figure;

        Vector2[] moves = { new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(1, 0),
        new Vector2(1, -1),
        new Vector2(0, -1),
        new Vector2(-1, -1),
        new Vector2(-1, 0),
        new Vector2(-1, 1)};

        for (int i = 0; i < moves.Length; i++)
        {
            int offset = 1;

            while (true)
            {
                Cell cell = Board.instance.GetCell(to.GetIndex() + moves[i] * offset);
                if (cell == null)
                    break;

                if (from!= null && cell.GetIndex() == from.GetIndex())
                {
                    offset++;
                    continue;
                }

                if (cell.HasFigureOnIt())
                {
                    if (cell.FigureOnIt() == king)
                    {
                        offset++;
                        continue;
                    }else if (cell.FigureOnIt().white == figure.white)
                    {
                        break;
                    }
                    else
                    {
                        if (cell.FigureOnIt().GetComponent<TowerMovement>() != null) // Tower check
                        {
                            if (i == 0 || i == 2 || i == 4 || i == 6)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (cell.FigureOnIt().GetComponent<BeshopMovement>() != null) // Bishop check
                        {
                            if (i == 1 || i == 3 || i == 5 || i == 7)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (cell.FigureOnIt().GetComponent<QueenMovement>() != null) // Queen check
                        {
                            return false;
                        }
                        if (cell.FigureOnIt().GetComponent<KingMovement>() != null) // King check
                        {
                            if (offset == 1)
                                return false;
                            else
                                break;
                        }
                    }
                    break;
                }
                offset++;
            }
        }

        Vector2[] knightMoves = { new Vector2(1, 2),
        new Vector2(2, 1),
        new Vector2(2, -1),
        new Vector2(1, -2),
        new Vector2(-1, -2),
        new Vector2(-2, -1),
        new Vector2(-2, 1),
        new Vector2(-1, 2)};

        for (int i = 0; i < knightMoves.Length; i++)
        {
            Cell cell = Board.instance.GetCell(to.GetIndex() + knightMoves[i]);
            if (cell == null)
                continue;

            if(cell.HasFigureOnIt() && cell.FigureOnIt().white != king.white && cell.FigureOnIt().GetComponent<KnightMovement>() != null)
            {
                return false;
            }
        }

        if (king.white) //PAWN
        {
            Cell pawnPos0 = Board.instance.GetCell(to.GetIndex() + new Vector2(1, 1));
            if (pawnPos0 != null && pawnPos0.HasFigureOnIt() && pawnPos0.FigureOnIt().white != king.white && pawnPos0.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
            Cell pawnPos1 = Board.instance.GetCell(to.GetIndex() + new Vector2(-1, 1));
            if (pawnPos1 != null && pawnPos1.HasFigureOnIt() && pawnPos1.FigureOnIt().white != king.white && pawnPos1.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
        }
        else
        {
            Cell pawnPos0 = Board.instance.GetCell(to.GetIndex() + new Vector2(1, -1));
            if (pawnPos0 != null && pawnPos0.HasFigureOnIt() && pawnPos0.FigureOnIt().white != king.white && pawnPos0.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
            Cell pawnPos1 = Board.instance.GetCell(to.GetIndex() + new Vector2(-1, -1));
            if (pawnPos1 != null && pawnPos1.HasFigureOnIt() && pawnPos1.FigureOnIt().white != king.white && pawnPos1.FigureOnIt().GetComponent<PawnMovement>() != null)
                return false;
        }

        return true;
    }
    public Figure GetFigureChechkingAtTheMoment()
    {
        return figureCheckingAtTheMoment;
    }
    public bool IsWhiteTurn()
    {
        
        return whiteTurn;
    }
    public IEnumerator WaitForCoronation(Cell cell)
    {
        coronating = true;
        yield return PawnCoronation.instance.ShowCoronationPanel();

        if (OnlineMultiplayer.instance != null && OnlineMultiplayer.instance.IsOnline())
        {
            PlayerController.instance.DoCoronation(GetFigureChechkingAtTheMoment().GetCell().GetIndex(), cell.GetIndex(), IsWhiteTurn(),
                PawnCoronation.instance.SelectedIndex()); ;
        }

        var coronated = Instantiate(PawnCoronation.instance.GetSelectedFigure(), cell.transform);
        coronated.GetComponent<Figure>().SetPlayer(cell.GetIndex().y == 7 ? true : false, cell.GetIndex().y == 7 ? ColorManager.instance.GetWhite() : ColorManager.instance.GetBlack());
        coronated.GetComponent<Figure>().MoveToCell(cell, true);

        figureCheckingAtTheMoment.EatThis();

        if(cell.GetIndex().y == 7)
        {
            StartCoroutine(WhiteMove());
        }
        else
        {
            StartCoroutine(BlackMove());
        }
        coronating = false;
    }
    public bool Coronating()
    {
        return coronating;
    }
    public void EnablePassing(bool state, Figure figure)
    {
        if (state)
        {
            passantAvailable = true;
            passantFigure = figure;
            return;
        }
        passantAvailable = false;
        passantFigure = null;
    }
    public bool Passant()
    {
        if (Modificators.instance != null && Modificators.instance.DoubleTurn())
            return false;
        return passantAvailable;
    }
    public Figure PassantFigure()
    {
        return passantFigure;
    }
    public IEnumerator WhiteMove()
    {
        yield return 0;
        if (CheckCheck(true))
        {
            AudioManager.instance.PlayCheck();

            turnsLeft = 0; //For Double mov modifier
            checkText.gameObject.SetActive(true);
            if (CheckCheckMate(true))
            {
                if (InitializeAdsScript.instance != null)
                    InitializeAdsScript.instance.ShowInterstitialAd();
                finished = true;
                endPanel.SetActive(true);
                winnerText.text = "Winner: white!";
            }
        }
        else
        {
            AudioManager.instance.PlayMovement();

            checkText.gameObject.SetActive(false);
            if (CheckCheckMate(true) || TieDueLackOfFigures())
            {
                if (InitializeAdsScript.instance != null)
                    InitializeAdsScript.instance.ShowInterstitialAd();
                finished = true;
                endPanel.SetActive(true);
                winnerText.text = "Tie!";
            }
        }

        if (Modificators.instance.DoubleTurn())
        {
            turnsLeft--;
            if(turnsLeft < 1)
            {
                turnsLeft = 2;
                if (timer != null)
                    timer.Turn(false);
                playerTurnText.text = "BLACK";

                whiteTurn = false;
            }
        }
        else
        {
            playerTurnText.text = "BLACK";

            if (timer != null)
                timer.Turn(false);
            whiteTurn = false;
        }

        
    }
    public IEnumerator BlackMove()
    {
        yield return 0;
        if (CheckCheck(false))
        {
            AudioManager.instance.PlayCheck();

            turnsLeft = 0; //For Double mov modifier
            checkText.gameObject.SetActive(true);
            if (CheckCheckMate(false))
            {
                if (InitializeAdsScript.instance != null)
                    InitializeAdsScript.instance.ShowInterstitialAd();
                finished = true;
                endPanel.SetActive(true);
                winnerText.text = "Winner: black!";
            }
        }
        else
        {
            AudioManager.instance.PlayMovement();

            checkText.gameObject.SetActive(false);
            if(CheckCheckMate(false) || TieDueLackOfFigures())
            {
                if (InitializeAdsScript.instance != null)
                    InitializeAdsScript.instance.ShowInterstitialAd();
                finished = true;
                endPanel.SetActive(true);
                winnerText.text = "Tie!";
            }
        }

        if (Modificators.instance.DoubleTurn())
        {
            turnsLeft--;
            if (turnsLeft < 1)
            {
                playerTurnText.text = "WHITE";

                turnsLeft = 2;
                if (timer != null)
                    timer.Turn(true);
                whiteTurn = true;
            }
        }
        else
        {
            playerTurnText.text = "WHITE";

            if (timer != null)
                timer.Turn(true);
            whiteTurn = true;
        }
    }
    public IEnumerator TimeOut(bool whiteTimeOut)
    {
        yield return 0;
        Timer.instance.StopTimer();
        endPanel.SetActive(true);
        winnerText.text = whiteTimeOut? "Winner: black!" : "Winner: white!";
    }
    public void CreateStandardFigureDisposition()
    {
        //Pawns
        for (int i = 0; i < 8; i++)
        {
            var pawn = Instantiate(pawnPrefab, Board.instance.GetBoard()[i, 1].transform);
            pawn.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
            pawn.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, 1], true);
        }

        for (int i = 0; i < 8; i++)
        {
            var pawn = Instantiate(pawnPrefab, Board.instance.GetBoard()[i, 6].transform);
            pawn.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
            pawn.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, 6], true);
        }

        //Towers
        var towerWhiteLeft = Instantiate(towerPrefab, Board.instance.GetBoard()[0, 0].transform);
        towerWhiteLeft.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        towerWhiteLeft.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[0, 0], true);

        var towerWhiteRight = Instantiate(towerPrefab, Board.instance.GetBoard()[7, 0].transform);
        towerWhiteRight.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        towerWhiteRight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[7, 0], true);

        var towerBlackLeft = Instantiate(towerPrefab, Board.instance.GetBoard()[0, 7].transform);
        towerBlackLeft.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        towerBlackLeft.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[0, 7], true);

        var towerBlackRight = Instantiate(towerPrefab, Board.instance.GetBoard()[7, 7].transform);
        towerBlackRight.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        towerBlackRight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[7, 7], true);

        //Beshops

        var bishopWhiteLeft = Instantiate(beshopPrefab, Board.instance.GetBoard()[2, 0].transform);
        bishopWhiteLeft.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        bishopWhiteLeft.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[2, 0], true);

        var bishopWhiteRight = Instantiate(beshopPrefab, Board.instance.GetBoard()[5, 0].transform);
        bishopWhiteRight.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        bishopWhiteRight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[5, 0], true);

        var bishopBlackLeft = Instantiate(beshopPrefab, Board.instance.GetBoard()[2, 7].transform);
        bishopBlackLeft.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        bishopBlackLeft.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[2, 7], true);

        var bishopBlackRight = Instantiate(beshopPrefab, Board.instance.GetBoard()[5, 7].transform);
        bishopBlackRight.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        bishopBlackRight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[5, 7], true);

        // Knights

        var knightWhiteLeft = Instantiate(knightPrefab, Board.instance.GetBoard()[1, 0].transform);
        knightWhiteLeft.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        knightWhiteLeft.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[1, 0], true);

        var knightWhiteRight = Instantiate(knightPrefab, Board.instance.GetBoard()[6, 0].transform);
        knightWhiteRight.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        knightWhiteRight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[6, 0], true);

        var knightBlackLeft = Instantiate(knightPrefab, Board.instance.GetBoard()[1, 7].transform);
        knightBlackLeft.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        knightBlackLeft.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[1, 7], true);

        var knightBlackRight = Instantiate(knightPrefab, Board.instance.GetBoard()[6, 7].transform);
        knightBlackRight.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        knightBlackRight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[6, 7], true);

        // Queens

        var queenWhite = Instantiate(queenPrefab, Board.instance.GetBoard()[3, 0].transform);
        queenWhite.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        queenWhite.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[3, 0], true);

        var queenBlack = Instantiate(queenPrefab, Board.instance.GetBoard()[3, 7].transform);
        queenBlack.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        queenBlack.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[3, 7], true);

        // Kings
        var kingWhite = Instantiate(kingPrefab, Board.instance.GetBoard()[4, 0].transform);
        kingWhite.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        kingWhite.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[4, 0], true);
        whiteKing = kingWhite.GetComponent<Figure>();

        var kingBlack = Instantiate(kingPrefab, Board.instance.GetBoard()[4, 7].transform);
        kingBlack.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        kingBlack.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[4, 7], true);
        blackKing = kingBlack.GetComponent<Figure>();
    }
    public void CreateCustomDisposition()
    {
        if(Modificators.instance == null)
        {
            Debug.LogError("modificator script not found");
            return;
        }

        if (Modificators.instance.AllFiguresAreBishops())
        {
            SetAllFiguresAs(beshopPrefab);
            return;
        }
        
        if (Modificators.instance.AllFiguresAreKnight())
        {
            SetAllFiguresAs(knightPrefab);
            return;
        }

        if (Modificators.instance.AllFiguresAreQueen())
        {
            SetAllFiguresAs(queenPrefab);
            return;
        }

        if (Modificators.instance.AllFiguresAreTowers())
        {
            SetAllFiguresAs(towerPrefab);
            return;
        }

        if (Modificators.instance.Random())
        {
            GameObject[] figures = new GameObject[] {towerPrefab, knightPrefab, beshopPrefab, queenPrefab, pawnPrefab };
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (i == 0 && j == 4)
                    {
                        var kingWhite = Instantiate(kingPrefab, Board.instance.GetBoard()[4, 0].transform);
                        kingWhite.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
                        kingWhite.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[4, 0], true);
                        whiteKing = kingWhite.GetComponent<Figure>();

                        var kingBlack = Instantiate(kingPrefab, Board.instance.GetBoard()[4, 7].transform);
                        kingBlack.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
                        kingBlack.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[4, 7], true);
                        blackKing = kingBlack.GetComponent<Figure>();
                        continue;
                    }


                    int randomIndex = Random.Range(0, figures.Length);

                    var figW = Instantiate(figures[randomIndex], Board.instance.GetBoard()[j, i].transform);
                    figW.GetComponent<Figure>().SetPlayer(true , ColorManager.instance.GetWhite());
                    figW.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[j, i], true);

                    var figB = Instantiate(figures[randomIndex], Board.instance.GetBoard()[j, i].transform);
                    figB.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
                    figB.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[j, i == 0? 7 : 6], true);
                }
            }
        }

        if (Modificators.instance.PawnInvasion())
        {
            CreateStandardFigureDisposition();
            for (int i = 2; i < 6; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    var pawn = Instantiate(pawnPrefab, Board.instance.GetBoard()[j, i].transform);
                    pawn.GetComponent<Figure>().SetPlayer(i < 4? true : false, i < 4 ? ColorManager.instance.GetWhite() : ColorManager.instance.GetBlack());
                    pawn.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[j, i], true);
                }
            }
            return;
        }

        if (Modificators.instance.CornerWar())
        {
            CreateNachoDisposition();
            return;
        }

    }

    public void CreateNachoDisposition()
    {
        // Kings
        var kingWhite = Instantiate(kingPrefab, Board.instance.GetBoard()[7, 0].transform);
        kingWhite.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        kingWhite.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[7, 0], true);
        whiteKing = kingWhite.GetComponent<Figure>();

        var kingBlack = Instantiate(kingPrefab, Board.instance.GetBoard()[0, 7].transform);
        kingBlack.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        kingBlack.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[0, 7], true);
        blackKing = kingBlack.GetComponent<Figure>();

        // Queens

        var queenWhite = Instantiate(queenPrefab, Board.instance.GetBoard()[6, 0].transform);
        queenWhite.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        queenWhite.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[6, 0], true);

        var queenWhite2 = Instantiate(queenPrefab, Board.instance.GetBoard()[7, 1].transform);
        queenWhite2.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
        queenWhite2.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[7, 1], true);

        var queenBlack = Instantiate(queenPrefab, Board.instance.GetBoard()[1, 7].transform);
        queenBlack.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        queenBlack.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[1, 7], true);

        var queenBlack2 = Instantiate(queenPrefab, Board.instance.GetBoard()[0, 6].transform);
        queenBlack2.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
        queenBlack2.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[0, 6], true);

        //Knights
        int j = 0;
        for (int i = 4; i < 8; i++)
        {
            var knight = Instantiate(knightPrefab, Board.instance.GetBoard()[i, j].transform);
            knight.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
            knight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, j], true);
            j++;
        }

        j = 4;
        for (int i = 0; i < 4; i++)
        {
            var knight = Instantiate(knightPrefab, Board.instance.GetBoard()[i, j].transform);
            knight.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
            knight.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, j], true);
            j++;
        }

        //Bishops
        j = 0;
        for (int i = 5; i < 8; i++)
        {
            var bishop = Instantiate(beshopPrefab, Board.instance.GetBoard()[i, j].transform);
            bishop.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
            bishop.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, j], true);
            j++;
        }

        j = 5;
        for (int i = 0; i < 3; i++)
        {
            var bishop = Instantiate(beshopPrefab, Board.instance.GetBoard()[i, j].transform);
            bishop.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
            bishop.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, j], true);
            j++;
        }

        //Towers
        j = 0;
        for (int i = 3; i < 8; i++)
        {
            var tower = Instantiate(towerPrefab, Board.instance.GetBoard()[i, j].transform);
            tower.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
            tower.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, j], true);
            j++;
        }

        j = 3;
        for (int i = 0; i < 5; i++)
        {
            var tower = Instantiate(towerPrefab, Board.instance.GetBoard()[i, j].transform);
            tower.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
            tower.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[i, j], true);
            j++;
        }
    }

    void SetAllFiguresAs(GameObject figurePrefab)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if ((i == 0 && j == 4) || (i == 3 && j == 4))
                {
                    if (i == 0)
                    {
                        var kingWhite = Instantiate(kingPrefab, Board.instance.GetBoard()[4, 0].transform);
                        kingWhite.GetComponent<Figure>().SetPlayer(true, ColorManager.instance.GetWhite());
                        kingWhite.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[4, 0], true);
                        whiteKing = kingWhite.GetComponent<Figure>();
                        continue;
                    }
                    else
                    {
                        var kingBlack = Instantiate(kingPrefab, Board.instance.GetBoard()[4, 7].transform);
                        kingBlack.GetComponent<Figure>().SetPlayer(false, ColorManager.instance.GetBlack());
                        kingBlack.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[4, 7], true);
                        blackKing = kingBlack.GetComponent<Figure>();
                        continue;
                    }

                }

                int index = i;
                if (i > 1)
                {
                    index = i == 2 ? 6 : 7;
                }

                var fig = Instantiate(figurePrefab, Board.instance.GetBoard()[j, index].transform);
                fig.GetComponent<Figure>().SetPlayer(index < 3 ? true : false, index < 3 ? ColorManager.instance.GetWhite() : ColorManager.instance.GetBlack());
                fig.GetComponent<Figure>().MoveToCell(Board.instance.GetBoard()[j, index], true);
            }
        }
    }
    public bool Finished()
    {
        return finished;
    }
    public void ShowEnd(bool winner)
    {
        finished = true;
        endPanel.SetActive(true);
        winnerText.text = winner? "Winner: white!" : "Winner: black!";
    }
    public Color GetWhite()
    {
        return ColorManager.instance.GetWhite();
    }
    public Color GetBlack()
    {
        return ColorManager.instance.GetBlack();
    }

    private void Start()
    {
        timer = Timer.instance;
    }
    #region singleton
    //Singleton
    public static GameLogic instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    #endregion
}
