enum Maybe(T:! Sized) {
    Just(T),
    Nothing
}
fn unwrap_or(m: Maybe(i32), default: i32) -> i32 {
    match m {
        Maybe.Just(v) => v,
        Maybe.Nothing => default
    }
}
fn main() -> i32 {
    unwrap_or(Maybe.Just(7), 0) + unwrap_or(Maybe.Nothing(i32), 3)
}
