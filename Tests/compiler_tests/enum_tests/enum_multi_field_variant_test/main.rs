enum Pair {
    Two(i32, i32),
    Zero
}
fn main() -> i32 {
    let p = Pair.Two(3, 7);
    match p {
        Pair.Two(a, b) => a + b,
        Pair.Zero => 0
    }
}
