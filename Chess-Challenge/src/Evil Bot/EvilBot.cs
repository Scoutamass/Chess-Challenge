﻿using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
    int[] pieceValues = {0, 100, 300, 300, 500, 900, 9999};
    bool white = false;
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        int[] values = new int[moves.Length];
        white = board.IsWhiteToMove;

        for(int i = 0; i < moves.GetLength(0); i++)
        {
            Move m = moves[i];
            int value = 0;
            //Don't move king
            if((int)m.MovePieceType == 6)
            {
                value -= 3;
                if(board.HasKingsideCastleRight(white) || board.HasQueensideCastleRight(white)) value -= 7;
            }
            board.MakeMove(m);//                         THIS IS WHERE THE MOVE HAPPENS!!!!!!!!!!!!!!!!!!!!!!!
            //Move forward
            value += (white ? 1 : -1) * (m.TargetSquare.Rank - m.StartSquare.Rank);
            //move back row pawns
            if((int)m.MovePieceType == 1 && (m.StartSquare.Rank == 2 || m.StartSquare.Rank == 7)) value += 2;
            //Take checkmates
            if(board.IsInCheckmate()) return m;
            //Take checks
            if(board.IsInCheck()) value += 75;
            //Promote pawns
            if(m.IsPromotion) value += pieceValues[(int)m.PromotionPieceType] - 100;
            //Castle
            if(m.IsCastles) value += 5;
            //Avoid Draw when winning
            if(board.IsDraw()) value -= (white ? 1 : -1) * evalBoard(board)/3;
            //protect pieces and endanger enemies'
            value += checkProtection(board, white);
            //value -= checkDanger(board, white);
            //minimize reactions
            Move[] movesBack = board.GetLegalMoves(false);
            value -= movesBack.Length;
            if(movesBack.Length > 0)
            {
                //Don't trade unfavorably
                int highestCaptured = 0;
                int movesBackBack = 0;
                for(int j = 0; j < movesBack.Length; j++)
                {
                    Move b = movesBack[j];
                    board.MakeMove(b);
                    movesBackBack += board.GetLegalMoves().Length;
                    //Avoid Draw when winning
                    if(board.IsDraw()) value -= (white ? 1 : -1) * evalBoard(board)/4;
                    //don't lose
                    if(board.IsInCheckmate()) value -= 10000;
                    bool check = false;
                    if(board.IsInCheck())
                    {
                        check = true;
                        value -= 200;
                    }
                    board.UndoMove(b);
                    if(board.SquareIsAttackedByOpponent(b.TargetSquare) && check)
                    {
                        value += 190;
                    }
                    highestCaptured = (int)Math.Max(highestCaptured, pieceValues[(int)b.CapturePieceType] * (board.SquareIsAttackedByOpponent(b.TargetSquare) ? 0.5 : 1));
                }
                //maximise options
                movesBackBack/=movesBack.Length;
                value += (int)(Math.Pow(movesBackBack, 2)/50);
                value += pieceValues[(int)m.CapturePieceType] - highestCaptured;
            }

            board.UndoMove(m);
            values[i] = value;
        }

        Move best = moves[0];
        int bestvalue = -999999;
        for(int i = 0; i < moves.Length; i++) if(values[i] > bestvalue)
        {
            bestvalue = values[i];
            best = moves[i];
        }
        board.MakeMove(best);
        return best;
    }

    int checkProtection(Board board, bool white)
    {
        const double friendlyMult = 0.01;
        const double enemyMult = 0.01;
        int score = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        foreach(PieceList pl in allPieces) foreach(Piece p in pl) if((int)p.PieceType != 6) score += (int)(((white ^ p.IsWhite) ? enemyMult : friendlyMult) * pieceValues[(int)p.PieceType]) * (board.SquareIsAttackedByOpponent(p.Square) ? 1 : 0);
        return score;
    }

    int checkDanger(Board board, bool white)
    {
        if(board.TrySkipTurn())
        {
            const double friendlyMult = 0.01;
            const double enemyMult = 0.005;
            int score = 0;
            PieceList[] allPieces = board.GetAllPieceLists();
            foreach(PieceList pl in allPieces) foreach(Piece p in pl) if((int)p.PieceType != 6)
            {
                score += (int)(((white ^ p.IsWhite) ? enemyMult : friendlyMult) * pieceValues[(int)p.PieceType]) * (board.SquareIsAttackedByOpponent(p.Square) ? 1 : 0);
            }
            board.UndoSkipTurn();
            return score;
        }
        return 0;
    }

    int evalBoard(Board board)
    {
        int score = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        foreach(PieceList pl in allPieces)
        {
            foreach(Piece p in pl) score += (p.IsWhite ? 1 : -1) * pieceValues[(int)p.PieceType];
        }
        return score;
    }
     
    }
}