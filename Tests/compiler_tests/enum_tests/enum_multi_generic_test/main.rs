enum Either(A:! type, B:! type) {
    Left(A),
    Right(B)
}
fn main() -> i32 {
    let x = Either(i32, i32).Left(5);
    match x {
        Either.Left(a) => a,
        Either.Right(b) => b
    }
}
