use Std.Ops.Add;

fn Add[T:! Sized +Add(T, Output = T) + Copy](a: T, b: T) -> T {
    a + b
}

fn main() -> i32 {
    Add(20, 22)
}
