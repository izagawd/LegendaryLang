struct Wrapper(T:! type) {
    val: T
}

impl[T:! type] Copy for Wrapper(T) {}

fn main() -> i32 {
    5
}
