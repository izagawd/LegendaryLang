trait Adder {
    fn add(a: Self, b: Self) -> Self;
}

trait Multiplier {
    fn mul(a: Self, b: Self) -> Self;
}

impl Adder for i32 {
    fn add(a: i32, b: i32) -> i32 {
        a + b
    }
}

impl Multiplier for i32 {
    fn mul(a: i32, b: i32) -> i32 {
        a * b
    }
}

fn compute<T: Adder + Multiplier + Copy>(a: T, b: T) -> T {
    <T as Adder>::add(<T as Multiplier>::mul(a, b), b)
}

fn main() -> i32 {
    compute::<i32>(3, 4)
}
