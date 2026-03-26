fn compute<T: Add<T> + Sub<T> + Copy>(one: T, two: T) -> T {
    one + two - two
}

fn main() -> i32 {
    compute::<i32>(10, 3)
}
