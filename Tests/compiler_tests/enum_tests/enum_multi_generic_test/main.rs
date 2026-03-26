enum Either<A, B> {
    Left(A),
    Right(B)
}
fn main() -> i32 {
    let x = Either::Left::<i32, i32>(5);
    match x {
        Either::Left(a) => a,
        Either::Right(b) => b
    }
}
