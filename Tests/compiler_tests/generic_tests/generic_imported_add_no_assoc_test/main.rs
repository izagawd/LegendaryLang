use Std.Ops.Add;

fn add_things[T:! Sized +Add(T) + Copy](a: T, b: T) -> (T as Std.Ops.Add(T)).Output {
    a + b
}

fn main() -> i32 {
    add_things(20, 22)
}
