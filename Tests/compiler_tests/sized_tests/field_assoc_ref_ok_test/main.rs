trait Producer: Sized {
    let Item :! MetaSized;
    fn produce(self: Self) -> &'static (Self as Producer).Item;
}

struct Holder['a](T:! Producer) {
    ptr: &'a (T as Producer).Item
}

fn main() -> i32 { 0 }
