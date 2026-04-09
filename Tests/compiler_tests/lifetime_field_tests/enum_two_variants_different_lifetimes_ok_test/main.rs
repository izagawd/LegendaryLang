enum Either['a, 'b] {
    Left(&'a i32),
    Right(&'b i32)
}
fn main() -> i32 {
    let x: i32 = 3;
    let y: i32 = 4;
    let e = Either.Left(&x);
    match e {
        Either.Left(r) => *r,
        Either.Right(r) => *r
    }
}
