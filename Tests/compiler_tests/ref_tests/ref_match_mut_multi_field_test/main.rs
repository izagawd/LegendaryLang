enum Pair {
    Two(i32, i32),
    None
}

fn double_both(p: &mut Pair) {
    match p {
        Pair.Two(a, b) => {
            *a = *a + *a;
            *b = *b + *b;
        },
        Pair.None => {}
    }
}

fn main() -> i32 {
    let p = Pair.Two(5, 7);
    double_both(&mut p);
    match p {
        Pair.Two(a, b) => a + b,
        Pair.None => 0
    }
}
