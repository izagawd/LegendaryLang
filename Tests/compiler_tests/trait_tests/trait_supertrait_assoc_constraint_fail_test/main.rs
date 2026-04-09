trait Producer {
    let Output :! type;
}

trait IntProducer: Producer(Output = i32) {}

impl Producer for i32 {
    let Output :! type = bool;
}

impl IntProducer for i32 {}

fn main() -> i32 {
    5
}
