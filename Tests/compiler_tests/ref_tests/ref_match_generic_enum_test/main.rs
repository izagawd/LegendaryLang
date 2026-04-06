enum Option(T:! type) {
    Some(T),
    None
}

fn unwrap_or(opt: &Option(i32), default: i32) -> i32 {
    match opt {
        Option.Some(x) => *x,
        Option.None => default
    }
}

fn main() -> i32 {
    let a = Option.Some(42);
    let b = Option(i32).None;
    unwrap_or(&a, 0) + unwrap_or(&b, 8)
}
