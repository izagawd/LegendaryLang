struct Wrapper(T:! Sized) {
    val: T
}

impl[T:! Sized] Copy for Wrapper(T) {}

fn main() -> i32 {
    5
}
