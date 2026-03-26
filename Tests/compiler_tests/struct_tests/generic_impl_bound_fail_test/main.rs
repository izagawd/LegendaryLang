struct NonCopy {
    val: i32
}

struct Wrapper<T> {
    val: T
}

trait Summable {
    fn sum(a: Self, b: Self) -> i32;
}

impl<T: Copy> Summable for Wrapper::<T> {
    fn sum(a: Wrapper::<T>, b: Wrapper::<T>) -> i32 {
        5
    }
}

fn main() -> i32 {
    let a = Wrapper::<NonCopy> { val = NonCopy { val = 1 } };
    let b = Wrapper::<NonCopy> { val = NonCopy { val = 2 } };
    <Wrapper::<NonCopy> as Summable>::sum(a, b)
}
