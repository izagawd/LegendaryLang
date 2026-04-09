use Std.Ops.PartialOrd;

fn max_of[T:! PartialOrd(T) + Copy](a: T, b: T) -> T {
    if a > b { a } else { b }
}

fn main() -> i32 {
    max_of(10, 20) + max_of(5, 3)
}
