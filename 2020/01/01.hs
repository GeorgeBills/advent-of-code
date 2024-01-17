import Data.List qualified as List
import Data.Set qualified as Set
import System.Environment as Environment

main = do
  args <- Environment.getArgs
  let f = if null args then "eg.txt" else head args
  input <- parseInput f
  print $ part1 sum input
  print $ part2 sum input
  where
    sum = 2020

parseInput :: FilePath -> IO [Int]
parseInput fp = do
  c <- readFile fp
  return $ map read $ lines c

part1 :: Int -> [Int] -> Maybe Int
part1 = partX 2

part2 :: Int -> [Int] -> Maybe Int
part2 = partX 3

partX :: Int -> Int -> [Int] -> Maybe Int
partX x sum ns = do
  xs <- findN x sum $ Set.fromList ns
  return $ product xs

findN :: Int -> Int -> Set.Set Int -> Maybe [Int]
findN n sum set
  | n < 1 = error "n must be positive"
  | n == 1 = if Set.member sum set then Just [sum] else Nothing
  | n > Set.size set = Nothing
  | otherwise =
      let (head, tail) = Set.deleteFindMin set
          sub = findN (n - 1) (sum - head) tail
       in case sub of
            Just sub -> Just (head : sub)
            Nothing -> findN n sum tail
