struct Wrapper<T> {
    val: T
}

trait HasValue {
    fn get_value(self_val: Self) -> i32;
}

impl HasValue for Wrapper<i32> {
    fn get_value(self_val: Wrapper<i32>) -> i32 {
        self_val.val
    }
}

fn get_it<T: HasValue>(thing: T) -> i32 {
    HasValue::get_value(thing)
}

fn main() -> i32 {
    let w = Wrapper::<i32> { val = 99 };
    get_it::<Wrapper<i32>>(w)
}
