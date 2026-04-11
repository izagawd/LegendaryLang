trait Producer: Sized {
    let Item :! type;
    fn produce(self: Self) -> &'static (Self as Producer).Item;
}

struct Holder['a](T:! Sized +Producer) {
    ptr: &'a (T as Producer).Item
}

fn main() -> i32 { 0 }
