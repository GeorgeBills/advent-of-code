import Data.Char (ord)
import Data.Maybe (fromMaybe)
import System.Environment (getArgs)

main = do
  args <- getArgs
  let f = if null args then "eg.txt" else head args
  input <- parseInput f
  print input

parseInput :: FilePath -> IO [Maybe (Int, Int, Char, String)]
parseInput fp = do
  c <- readFile fp
  return $ map parseLine $ lines c

parseLine :: String -> Maybe (Int, Int, Char, String)
parseLine l = do
  (x, l) <- parseInt l
  (_, l) <- parseDash l
  (y, l) <- parseInt l
  (_, l) <- parseSpace l
  (c, l) <- parseChar l
  (_, l) <- parseColon l
  (_, l) <- parseSpace l
  pw <- parseString l
  return (x, y, c, pw)

parseInt :: String -> Maybe (Int, String)
parseInt = parseInt' Nothing
  where
    parseInt' :: Maybe Int -> String -> Maybe (Int, String)
    parseInt' (Just n) "" = Just (n, "")
    parseInt' Nothing "" = Nothing
    parseInt' m (x : xs)
      | '0' <= x && x <= '9' =
          let d = ord x - ord '0'
              n = fromMaybe 0 m
              a = Just (n * 10 + d)
           in parseInt' a xs
      | otherwise = case m of
          Just n -> Just (n, x : xs)
          Nothing -> Nothing

parseChar :: String -> Maybe (Char, String)
parseChar "" = Nothing
parseChar (x : xs) = Just (x, xs)

parseCharC :: Char -> String -> Maybe (Char, String)
parseCharC c "" = Nothing
parseCharC c (x : xs) = if c == x then Just (x, xs) else Nothing

parseDash = parseCharC '-'

parseSpace = parseCharC ' '

parseColon = parseCharC ':'

parseString :: String -> Maybe String
parseString "" = Nothing
parseString s = Just s
