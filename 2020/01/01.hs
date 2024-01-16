import qualified Data.List as List
import qualified Data.Set as Set

want :: Int
want = 2020

main = do
  input <- readInput "in.txt" -- throws?!
  putStrLn $ show input

  let find2' = find2 want
  let pair = find2' input
  putStrLn $ show pair

  let answer = fmap (\(x, y) -> x * y) pair
  putStrLn $ show answer

readInput :: FilePath -> IO [Int]
readInput filePath = do
  contents <- readFile filePath
  return $ map read $ lines contents

find2 :: Int -> [Int] -> Maybe (Int, Int)
find2 n ns =
  case x1 of
    Just x  -> Just (x, n - x)
    Nothing -> Nothing
  where
    set = Set.fromList ns
    pred = \x -> Set.member (n - x) set
    x1 = List.find pred ns
