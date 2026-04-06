fn unwrap_add(a: &Option(i32), b: &Option(i32)) -> i32 {
    let va = match a {
        Option.Some(x) => *x,
        Option.None => 0
    };
    let vb = match b {
        Option.Some(x) => *x,
        Option.None => 0
    };
    va + vb
}

fn main() -> i32 {
    let a = Option.Some(30);
    let b = Option.Some(12);
    let c = Option(i32).None;
    unwrap_add(&a, &b) + unwrap_add(&a, &c)
}
