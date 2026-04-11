struct Wrapper(T:! Sized) {
    val: T
}

impl[T:! Sized +Copy] Copy for Wrapper(T) {}

fn idk(T:! Sized +Copy, input: T) -> i32 {
    let made = make Wrapper(T) { val : input };
    let move_here = made;
    let should_copy = made;
    5
}

fn main() -> i32 {
    idk(i32, 10)
}
