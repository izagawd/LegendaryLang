enum Pair {
    Two(i32, i32),
    None
}

fn sum_pair(p: &Pair) -> i32 {
    match p {
        Pair.Two(a, b) => *a + *b,
        Pair.None => 0
    }
}

fn main() -> i32 {
    let p = Pair.Two(13, 29);
    sum_pair(&p)
}
