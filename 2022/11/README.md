The puzzle description for part two drops pretty big hints:

> You're worried you might not ever get your items back. So worried, in fact,
> that your relief that a monkey's inspection didn't damage an item no longer
> causes your worry level to be divided by three.
>
> Unfortunately, that relief was **all that was keeping your worry levels from
> reaching ridiculous levels**. You'll **need to find another way to keep your
> worry levels manageable**.
>
> ...
>
> Worry levels are no longer divided by three after each item is inspected;
> you'll **need to find another way to keep your worry levels manageable**.

The puzzle answer we get is wrong, and if we debug we get negative worry
numbers. Negative worry numbers shouldn't be possible, so ask C# to check for
wrapping and we get an `OverflowException`.

```xml
<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>
```

> Unhandled exception. System.OverflowException: Arithmetic operation resulted
> in an overflow.

This should have been more obvious... multiplying an int, possibly multiple
times, over 10k rounds will result in a very large number.

The trick is that we only care if the worry level is evenly divisible by a given
(unchanging) divisor, and `worry%divisor==0` is the same thing as
`worry%divisor%divisor==0`.

We have different divisors as we rotate through the monkeys, so we wrap our
worry level by the product of all those divisors; `worry%divisorX==0` is the
same thing as `worry%(divisorA*divisorB*divisorC)==0`.

The divisors are all primes, so I don't think we can simplify them down at all.

| monkey | operation      | divisor | item | round 1  | test(r1) | r1%divisor | test(r1%divisor) | r1%(2*3) | test(r1%(2*3)) |
| ------ | -------------- | ------- | ---- | -------- | -------- | ---------- | ---------------- | -------- | -------------- |
| 1      | new = old * 13 | 2       | 57   | 741      | FALSE    | 1          | FALSE            | 3        | FALSE          |
| 1      | new = old * 13 | 2       | 95   | 1235     | FALSE    | 1          | FALSE            | 5        | FALSE          |
| 1      | new = old * 13 | 2       | 80   | 1040     | TRUE     | 0          | TRUE             | 2        | TRUE           |
| 1      | new = old * 13 | 2       | 92   | 1196     | TRUE     | 0          | TRUE             | 2        | TRUE           |
| 1      | new = old * 13 | 2       | 57   | 741      | FALSE    | 1          | FALSE            | 3        | FALSE          |
| 1      | new = old * 13 | 2       | 78   | 1014     | TRUE     | 0          | TRUE             | 0        | TRUE           |
| 6      | new = old + 2  | 3       | 61   | 63       | TRUE     | 0          | TRUE             | 3        | TRUE           |
| 6      | new = old + 2  | 3       | 54   | 56       | FALSE    | 2          | FALSE            | 2        | FALSE          |
| 6      | new = old + 2  | 3       | 94   | 96       | TRUE     | 0          | TRUE             | 0        | TRUE           |
| 6      | new = old + 2  | 3       | 71   | 73       | FALSE    | 1          | FALSE            | 1        | FALSE          |
| 6      | new = old + 2  | 3       | 74   | 76       | FALSE    | 1          | FALSE            | 4        | FALSE          |
| 6      | new = old + 2  | 3       | 68   | 70       | FALSE    | 1          | FALSE            | 4        | FALSE          |
| 6      | new = old + 2  | 3       | 98   | 100      | FALSE    | 1          | FALSE            | 4        | FALSE          |
| 6      | new = old + 2  | 3       | 83   | 85       | FALSE    | 1          | FALSE            | 1        | FALSE          |
