struct NonCopy {
    val: i32
}

struct Wrapper(T:! Sized) {
    val: T
}

trait Summable: Sized {
    fn sum(a: Self, b: Self) -> i32;
}

impl[T:! Sized +Copy] Summable for Wrapper(T) {
    fn sum(a: Wrapper(T), b: Wrapper(T)) -> i32 {
        5
    }
}

fn main() -> i32 {
    let a = make Wrapper(NonCopy) { val : make NonCopy { val : 1 } };
    let b = make Wrapper(NonCopy) { val : make NonCopy { val : 2 } };
    (Wrapper(NonCopy) as Summable).sum(a, b)
}
